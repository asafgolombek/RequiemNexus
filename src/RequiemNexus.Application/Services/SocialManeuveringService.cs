using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Persists and mutates <see cref="SocialManeuver"/> entities with Masquerade checks and server-side dice.
/// </summary>
public class SocialManeuveringService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    IDiceService diceService,
    ISessionPublisher sessionPublisher,
    ILogger<SocialManeuveringService> logger) : ISocialManeuveringService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IDiceService _diceService = diceService;
    private readonly ISessionPublisher _sessionPublisher = sessionPublisher;
    private readonly ILogger<SocialManeuveringService> _logger = logger;

    /// <inheritdoc />
    public async Task<SocialManeuver> CreateAsync(
        int campaignId,
        int initiatorCharacterId,
        int targetChronicleNpcId,
        string goalDescription,
        bool goalWouldBeBreakingPoint,
        bool goalPreventsAspiration,
        bool actsAgainstVirtueOrMask,
        string storytellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storytellerUserId, "create a Social maneuver");

        if (string.IsNullOrWhiteSpace(goalDescription))
        {
            throw new InvalidOperationException("Goal description is required.");
        }

        Character initiator = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == initiatorCharacterId)
            ?? throw new InvalidOperationException($"Character {initiatorCharacterId} not found.");

        if (initiator.CampaignId != campaignId)
        {
            throw new InvalidOperationException("Initiator must belong to the campaign.");
        }

        ChronicleNpc target = await _dbContext.ChronicleNpcs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == targetChronicleNpcId)
            ?? throw new InvalidOperationException($"Chronicle NPC {targetChronicleNpcId} not found.");

        if (target.CampaignId != campaignId)
        {
            throw new InvalidOperationException("Target NPC must belong to the campaign.");
        }

        bool burntExists = await _dbContext.SocialManeuvers.AnyAsync(m =>
            m.InitiatorCharacterId == initiatorCharacterId
            && m.TargetChronicleNpcId == targetChronicleNpcId
            && m.Status == ManeuverStatus.Burnt);

        if (burntExists)
        {
            throw new InvalidOperationException(
                "This initiator has burnt the relationship with that NPC; Social maneuvering cannot begin again.");
        }

        (int resolve, int composure) = SocialManeuveringAttributeParser.ReadResolveComposure(target.AttributesJson);
        int initialDoors = SocialManeuveringEngine.ComputeInitialDoorCount(
            resolve,
            composure,
            goalWouldBeBreakingPoint,
            goalPreventsAspiration,
            actsAgainstVirtueOrMask);

        SocialManeuver maneuver = new()
        {
            CampaignId = campaignId,
            InitiatorCharacterId = initiatorCharacterId,
            TargetChronicleNpcId = targetChronicleNpcId,
            GoalDescription = goalDescription.Trim(),
            InitialDoors = initialDoors,
            RemainingDoors = initialDoors,
            CurrentImpression = ImpressionLevel.Average,
            Status = ManeuverStatus.Active,
        };

        _dbContext.SocialManeuvers.Add(maneuver);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Social maneuver {ManeuverId} created in campaign {CampaignId} (initiator {CharacterId}, target NPC {NpcId}, doors {Doors}) by ST {UserId}",
            maneuver.Id,
            campaignId,
            initiatorCharacterId,
            targetChronicleNpcId,
            initialDoors,
            storytellerUserId);

        await PublishManeuverUpdateAsync(maneuver.Id);

        return maneuver;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SocialManeuver>> ListForCampaignAsync(int campaignId, string storytellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storytellerUserId, "list Social maneuvers");

        return await _dbContext.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .Where(m => m.CampaignId == campaignId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SocialManeuver>> ListForInitiatorAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "view Social maneuvers");

        return await _dbContext.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .Where(m => m.InitiatorCharacterId == characterId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(SocialManeuver Updated, RollResult Roll, int DoorsOpened)> RollOpenDoorAsync(
        int maneuverId,
        int dicePool,
        string userId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
        await RequireInitiatorOrStorytellerAsync(maneuver, userId, "roll to open a Door");

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

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Open-Door roll on maneuver {ManeuverId}: successes={Successes}, doorsOpened={DoorsOpened}, remaining={Remaining}, user {UserId}",
            maneuverId,
            roll.Successes,
            doorsOpened,
            maneuver.RemainingDoors,
            userId);

        await PublishManeuverUpdateAsync(maneuver.Id);

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
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
        await RequireInitiatorOrStorytellerAsync(maneuver, userId, "force Doors");

        if (maneuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("This maneuver is no longer active.");
        }

        int closedDoors = maneuver.RemainingDoors;

        if (applyHardLeverage)
        {
            Character initiator = await _dbContext.Characters
                .AsNoTracking()
                .FirstAsync(c => c.Id == maneuver.InitiatorCharacterId);

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

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Force-Doors roll on maneuver {ManeuverId}: successes={Successes}, success={ForcedSuccess}, status={Status}, user {UserId}",
            maneuverId,
            roll.Successes,
            forcedSuccess,
            maneuver.Status,
            userId);

        await PublishManeuverUpdateAsync(maneuver.Id);

        return (maneuver, roll, forcedSuccess);
    }

    /// <inheritdoc />
    public async Task SetImpressionAsync(int maneuverId, ImpressionLevel impression, string storytellerUserId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
        await _authHelper.RequireStorytellerAsync(maneuver.CampaignId, storytellerUserId, "set maneuver impression");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (impression == ImpressionLevel.Hostile && maneuver.CurrentImpression != ImpressionLevel.Hostile)
        {
            maneuver.HostileSince = now;
        }
        else if (impression != ImpressionLevel.Hostile)
        {
            maneuver.HostileSince = null;
        }

        maneuver.CurrentImpression = impression;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver {ManeuverId} impression set to {Impression} by ST {UserId}",
            maneuverId,
            impression,
            storytellerUserId);

        await PublishManeuverUpdateAsync(maneuver.Id);
    }

    /// <inheritdoc />
    public async Task SetRemainingDoorsNarrativeAsync(int maneuverId, int remainingDoors, string storytellerUserId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
        await _authHelper.RequireStorytellerAsync(maneuver.CampaignId, storytellerUserId, "adjust maneuver Doors");

        if (remainingDoors < 0 || remainingDoors > maneuver.InitialDoors)
        {
            throw new InvalidOperationException(
                $"Remaining Doors must be between 0 and {maneuver.InitialDoors}.");
        }

        maneuver.RemainingDoors = remainingDoors;

        if (maneuver.Status == ManeuverStatus.Active && maneuver.RemainingDoors <= 0)
        {
            maneuver.Status = ManeuverStatus.Succeeded;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver {ManeuverId} remaining Doors set to {Remaining} by ST {UserId}",
            maneuverId,
            remainingDoors,
            storytellerUserId);

        await PublishManeuverUpdateAsync(maneuver.Id);
    }

    private async Task PublishManeuverUpdateAsync(int maneuverId)
    {
        SocialManeuver? row = await _dbContext.SocialManeuvers
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

    private async Task<SocialManeuver> LoadManeuverForMutationAsync(int maneuverId)
    {
        SocialManeuver maneuver = await _dbContext.SocialManeuvers
            .FirstOrDefaultAsync(m => m.Id == maneuverId)
            ?? throw new InvalidOperationException($"Social maneuver {maneuverId} not found.");

        await ApplyHostileWeekFailureIfNeededAsync(maneuver, DateTimeOffset.UtcNow);
        return maneuver;
    }

    private async Task ApplyHostileWeekFailureIfNeededAsync(SocialManeuver maneuver, DateTimeOffset nowUtc)
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

        await _dbContext.SaveChangesAsync();
        await PublishManeuverUpdateAsync(maneuver.Id);
    }

    private async Task RequireInitiatorOrStorytellerAsync(SocialManeuver maneuver, string userId, string operationName)
    {
        Character initiator = await _dbContext.Characters
            .AsNoTracking()
            .FirstAsync(c => c.Id == maneuver.InitiatorCharacterId);

        bool isOwner = initiator.ApplicationUserId == userId;
        bool isStoryteller = await _dbContext.Campaigns.AnyAsync(
            c => c.Id == maneuver.CampaignId && c.StoryTellerId == userId);

        if (!isOwner && !isStoryteller)
        {
            _logger.LogWarning(
                "Unauthorized attempt to {OperationName} on maneuver {ManeuverId} by user {UserId}",
                operationName,
                maneuver.Id,
                userId);

            throw new UnauthorizedAccessException(
                $"Only the initiating character's owner or the Storyteller may {operationName}.");
        }
    }
}
