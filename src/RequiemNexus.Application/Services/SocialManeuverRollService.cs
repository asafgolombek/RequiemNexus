using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Dice-pool rolls for Social maneuvering: Open Door and Force Doors.
/// General mutations are handled by <see cref="SocialManeuveringService"/>.
/// </summary>
public class SocialManeuverRollService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    IDiceService diceService,
    IConditionService conditionService,
    ISessionPublisher sessionPublisher,
    ILogger<SocialManeuverRollService> logger) : ISocialManeuverRollService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IDiceService _diceService = diceService;
    private readonly IConditionService _conditionService = conditionService;
    private readonly ISessionPublisher _sessionPublisher = sessionPublisher;
    private readonly ILogger<SocialManeuverRollService> _logger = logger;

    /// <inheritdoc />
    public async Task<(SocialManeuver Updated, RollResult Roll, int DoorsOpened)> RollOpenDoorAsync(
        int maneuverId,
        int dicePool,
        string userId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await LoadManeuverForMutationAsync(db, maneuverId);
        await _authHelper.RequireCharacterAccessAsync(maneuver.InitiatorCharacterId, userId, "roll to open a Door");

        if (maneuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("This maneuver is no longer active.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Result<bool> timing = SocialManeuveringEngine.ValidateOpenDoorRollTiming(
            maneuver.LastRollAt,
            maneuver.CurrentImpression,
            now);

        if (!timing.IsSuccess)
        {
            throw new InvalidOperationException(timing.Error);
        }

        int effectivePool = dicePool - maneuver.CumulativePenaltyDice;
        RollResult roll = _diceService.Roll(effectivePool);

        int doorsOpened = SocialManeuveringEngine.GetDoorsOpenedByOpenDoorRoll(
            roll.Successes,
            roll.IsExceptionalSuccess,
            roll.IsDramaticFailure);

        if (doorsOpened == 0 && !roll.IsDramaticFailure)
        {
            maneuver.CumulativePenaltyDice++;
        }

        maneuver.LastRollAt = now;
        maneuver.RemainingDoors = Math.Max(0, maneuver.RemainingDoors - doorsOpened);

        if (maneuver.RemainingDoors <= 0)
        {
            maneuver.Status = ManeuverStatus.Succeeded;
        }

        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Open-Door roll on maneuver {ManeuverId}: successes={Successes}, doorsOpened={DoorsOpened}, remaining={Remaining}, user {UserId}",
            maneuverId,
            roll.Successes,
            doorsOpened,
            maneuver.RemainingDoors,
            userId);

        await ApplyOpenDoorOutcomeConditionsAsync(db, maneuver, roll, doorsOpened, userId);

        await PublishManeuverUpdateAsync(db, maneuver.Id);

        return (maneuver, roll, doorsOpened);
    }

    /// <inheritdoc />
    public async Task<(SocialManeuver Updated, RollResult Roll, bool ForcedSuccess)> RollForceDoorsAsync(
        int maneuverId,
        int dicePool,
        bool applyHardLeverage,
        int breakingPointSeverity,
        string userId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await LoadManeuverForMutationAsync(db, maneuverId);
        await _authHelper.RequireCharacterAccessAsync(maneuver.InitiatorCharacterId, userId, "force Doors");

        if (maneuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("This maneuver is no longer active.");
        }

        int closedDoors = maneuver.RemainingDoors;

        if (applyHardLeverage)
        {
            Character initiator = await db.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == maneuver.InitiatorCharacterId)
                ?? throw new InvalidOperationException($"Character {maneuver.InitiatorCharacterId} not found.");

            Result<int> removed = SocialManeuveringEngine.ComputeHardLeverageDoorsRemoved(
                breakingPointSeverity,
                initiator.Humanity);

            if (!removed.IsSuccess)
            {
                throw new InvalidOperationException(removed.Error);
            }

            closedDoors = Math.Max(0, closedDoors - removed.Value!);
        }

        int penalty = SocialManeuveringEngine.ComputeForceRollPoolPenalty(closedDoors);
        int effectivePool = dicePool - penalty;
        RollResult roll = _diceService.Roll(effectivePool);

        bool forcedSuccess = roll.Successes >= 1 && !roll.IsDramaticFailure;
        maneuver.LastRollAt = DateTimeOffset.UtcNow;

        if (forcedSuccess)
        {
            maneuver.RemainingDoors = 0;
            maneuver.Status = ManeuverStatus.Succeeded;
        }
        else
        {
            maneuver.Status = ManeuverStatus.Burnt;
        }

        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Force-Doors roll on maneuver {ManeuverId}: successes={Successes}, success={ForcedSuccess}, status={Status}, user {UserId}",
            maneuverId,
            roll.Successes,
            forcedSuccess,
            maneuver.Status,
            userId);

        if (!forcedSuccess)
        {
            await ApplySocialConditionIfAbsentAsync(
                db,
                maneuver.InitiatorCharacterId,
                ConditionType.Shaken,
                "From failing to force Doors in Social maneuvering — the relationship is burnt.",
                userId);
        }
        else
        {
            string npcName = maneuver.TargetNpc?.Name ?? "the target";
            await ApplySocialConditionIfAbsentAsync(
                db,
                maneuver.InitiatorCharacterId,
                ConditionType.Swooned,
                $"From Social maneuver success (forced Doors) toward {npcName}.",
                userId);
        }

        await PublishManeuverUpdateAsync(db, maneuver.Id);

        return (maneuver, roll, forcedSuccess);
    }

    private async Task PublishManeuverUpdateAsync(ApplicationDbContext db, int maneuverId)
    {
        SocialManeuver? row = await db.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .FirstOrDefaultAsync(m => m.Id == maneuverId);

        if (row == null)
        {
            return;
        }

        var dto = new SocialManeuverUpdateDto(
            row.CampaignId,
            row.Id,
            row.InitiatorCharacterId,
            row.InitiatorCharacter?.Name ?? "?",
            row.TargetChronicleNpcId,
            row.TargetNpc?.Name ?? "?",
            row.RemainingDoors,
            row.InitialDoors,
            row.CurrentImpression,
            row.Status,
            row.CumulativePenaltyDice,
            row.LastRollAt,
            row.GoalDescription);

        await _sessionPublisher.Group(row.CampaignId).ReceiveSocialManeuverUpdate(dto);
    }

    private async Task<SocialManeuver> LoadManeuverForMutationAsync(ApplicationDbContext db, int maneuverId)
    {
        SocialManeuver maneuver = await db.SocialManeuvers
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .FirstOrDefaultAsync(m => m.Id == maneuverId)
            ?? throw new InvalidOperationException($"Social maneuver {maneuverId} not found.");

        await ApplyHostileWeekFailureIfNeededAsync(db, maneuver, DateTimeOffset.UtcNow);
        return maneuver;
    }

    private async Task ApplyHostileWeekFailureIfNeededAsync(ApplicationDbContext db, SocialManeuver maneuver, DateTimeOffset nowUtc)
    {
        if (maneuver.Status != ManeuverStatus.Active)
        {
            return;
        }

        if (!SocialManeuveringEngine.ShouldFailFromHostileWeek(maneuver.HostileSince, maneuver.CurrentImpression, nowUtc))
        {
            return;
        }

        maneuver.Status = ManeuverStatus.Failed;
        _logger.LogInformation(
            "Maneuver {ManeuverId} failed: Hostile impression persisted for one week.",
            maneuver.Id);

        await db.SaveChangesAsync();

        string stUserId = maneuver.Campaign?.StoryTellerId
            ?? await db.Campaigns.AsNoTracking()
                .Where(c => c.Id == maneuver.CampaignId)
                .Select(c => c.StoryTellerId)
                .FirstAsync();

        await ApplySocialConditionIfAbsentAsync(
            db,
            maneuver.InitiatorCharacterId,
            ConditionType.Shaken,
            "From Social maneuver failure: Hostile impression lasted a week.",
            stUserId);

        await PublishManeuverUpdateAsync(db, maneuver.Id);
    }

    private async Task ApplyOpenDoorOutcomeConditionsAsync(
        ApplicationDbContext db,
        SocialManeuver maneuver,
        RollResult roll,
        int doorsOpened,
        string actingUserId)
    {
        if (maneuver.Status == ManeuverStatus.Succeeded)
        {
            string npcName = maneuver.TargetNpc?.Name ?? "the target";
            await ApplySocialConditionIfAbsentAsync(
                db,
                maneuver.InitiatorCharacterId,
                ConditionType.Swooned,
                $"From Social maneuver success toward {npcName}.",
                actingUserId);
            return;
        }

        if (roll.IsDramaticFailure)
        {
            await ApplySocialConditionIfAbsentAsync(
                db,
                maneuver.InitiatorCharacterId,
                ConditionType.Shaken,
                "From a dramatic failure while opening a Door in Social maneuvering.",
                actingUserId);
            return;
        }

        if (roll.IsExceptionalSuccess && doorsOpened > 0)
        {
            await ApplySocialConditionIfAbsentAsync(
                db,
                maneuver.InitiatorCharacterId,
                ConditionType.Inspired,
                "From an exceptional success while opening a Door in Social maneuvering.",
                actingUserId);
        }
    }

    private async Task ApplySocialConditionIfAbsentAsync(
        ApplicationDbContext db,
        int characterId,
        ConditionType type,
        string? descriptionOverride,
        string actingUserId)
    {
        bool hasActive = await db.CharacterConditions.AnyAsync(
            c => c.CharacterId == characterId && !c.IsResolved && c.ConditionType == type);

        if (hasActive)
        {
            return;
        }

        await _conditionService.ApplyConditionAsync(characterId, type, null, descriptionOverride, actingUserId);
    }
}
