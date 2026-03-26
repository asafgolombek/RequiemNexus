using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Observability;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
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
    ISocialManeuverLifecycleCoordinator lifecycleCoordinator,
    ILogger<SocialManeuverRollService> logger) : ISocialManeuverRollService
{
    private const int _maxDeclaredDicePool = 50;

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IDiceService _diceService = diceService;
    private readonly ISocialManeuverLifecycleCoordinator _lifecycle = lifecycleCoordinator;
    private readonly ILogger<SocialManeuverRollService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<(SocialManeuver Updated, RollResult Roll, int DoorsOpened)>> RollOpenDoorAsync(
        int maneuverId,
        int dicePool,
        string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })!)
        {
            await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

            SocialManeuver maneuver = await _lifecycle.LoadManeuverForMutationAsync(db, maneuverId);
            await _authHelper.RequireCharacterAccessAsync(maneuver.InitiatorCharacterId, userId, "roll to open a Door");

            Result<int> poolCheck = await ValidateDeclaredDicePoolAsync(db, maneuver, dicePool, userId);
            if (!poolCheck.IsSuccess)
            {
                return Result<(SocialManeuver, RollResult, int)>.Failure(poolCheck.Error!);
            }

            if (maneuver.Status != ManeuverStatus.Active)
            {
                return Result<(SocialManeuver, RollResult, int)>.Failure("This maneuver is no longer active.");
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            Result<bool> timing = SocialManeuveringEngine.ValidateOpenDoorRollTiming(
                maneuver.LastRollAt,
                maneuver.CurrentImpression,
                now);

            if (!timing.IsSuccess)
            {
                return Result<(SocialManeuver, RollResult, int)>.Failure(timing.Error!);
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
                "Open-Door roll on maneuver {ManeuverId}: successes={Successes}, doorsOpened={DoorsOpened}, remaining={Remaining}, user {UserId} {CorrelationId}",
                maneuverId,
                roll.Successes,
                doorsOpened,
                maneuver.RemainingDoors,
                userId,
                correlationId);

            await ApplyOpenDoorOutcomeConditionsAsync(db, maneuver, roll, doorsOpened, userId);

            await _lifecycle.PublishManeuverUpdateAsync(db, maneuver.Id);

            return Result<(SocialManeuver, RollResult, int)>.Success((maneuver, roll, doorsOpened));
        }
    }

    /// <inheritdoc />
    public async Task<Result<(SocialManeuver Updated, RollResult Roll, bool ForcedSuccess)>> RollForceDoorsAsync(
        int maneuverId,
        int dicePool,
        bool applyHardLeverage,
        int breakingPointSeverity,
        string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })!)
        {
            await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

            SocialManeuver maneuver = await _lifecycle.LoadManeuverForMutationAsync(db, maneuverId);
            await _authHelper.RequireCharacterAccessAsync(maneuver.InitiatorCharacterId, userId, "force Doors");

            Result<int> poolCheck = await ValidateDeclaredDicePoolAsync(db, maneuver, dicePool, userId);
            if (!poolCheck.IsSuccess)
            {
                return Result<(SocialManeuver, RollResult, bool)>.Failure(poolCheck.Error!);
            }

            if (maneuver.Status != ManeuverStatus.Active)
            {
                return Result<(SocialManeuver, RollResult, bool)>.Failure("This maneuver is no longer active.");
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
                    return Result<(SocialManeuver, RollResult, bool)>.Failure(removed.Error!);
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
                "Force-Doors roll on maneuver {ManeuverId}: successes={Successes}, success={ForcedSuccess}, status={Status}, user {UserId} {CorrelationId}",
                maneuverId,
                roll.Successes,
                forcedSuccess,
                maneuver.Status,
                userId,
                correlationId);

            if (!forcedSuccess)
            {
                await _lifecycle.ApplySocialConditionIfAbsentAsync(
                    db,
                    maneuver.InitiatorCharacterId,
                    ConditionType.Shaken,
                    "From failing to force Doors in Social maneuvering — the relationship is burnt.",
                    userId);
            }
            else
            {
                string npcName = maneuver.TargetNpc?.Name ?? "the target";
                await _lifecycle.ApplySocialConditionIfAbsentAsync(
                    db,
                    maneuver.InitiatorCharacterId,
                    ConditionType.Swooned,
                    $"From Social maneuver success (forced Doors) toward {npcName}.",
                    userId);
            }

            await _lifecycle.PublishManeuverUpdateAsync(db, maneuver.Id);

            return Result<(SocialManeuver, RollResult, bool)>.Success((maneuver, roll, forcedSuccess));
        }
    }

    private async Task<Result<int>> ValidateDeclaredDicePoolAsync(
        ApplicationDbContext db,
        SocialManeuver maneuver,
        int dicePool,
        string userId)
    {
        if (dicePool < 0 || dicePool > _maxDeclaredDicePool)
        {
            return Result<int>.Failure($"Dice pool must be between 0 and {_maxDeclaredDicePool}.");
        }

        bool isStoryteller = maneuver.Campaign?.StoryTellerId == userId;
        if (isStoryteller)
        {
            return Result<int>.Success(0);
        }

        Character? initiator = await SocialManeuverDicePoolAuthority.LoadInitiatorForDiceCapAsync(
            db,
            maneuver.InitiatorCharacterId);

        if (initiator == null)
        {
            return Result<int>.Failure($"Character {maneuver.InitiatorCharacterId} not found.");
        }

        int maxFromSheet = SocialManeuverDicePoolAuthority.GetMaximumSocialDicePool(initiator);
        if (dicePool > maxFromSheet)
        {
            return Result<int>.Failure(
                $"Declared dice pool ({dicePool}) exceeds the initiator's largest applicable Attribute + Skill pool ({maxFromSheet}) on the sheet. The Storyteller may roll a higher pool when acting for the table.");
        }

        return Result<int>.Success(0);
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
            await _lifecycle.ApplySocialConditionIfAbsentAsync(
                db,
                maneuver.InitiatorCharacterId,
                ConditionType.Swooned,
                $"From Social maneuver success toward {npcName}.",
                actingUserId);
            return;
        }

        if (roll.IsDramaticFailure)
        {
            await _lifecycle.ApplySocialConditionIfAbsentAsync(
                db,
                maneuver.InitiatorCharacterId,
                ConditionType.Shaken,
                "From a dramatic failure while opening a Door in Social maneuvering.",
                actingUserId);
            return;
        }

        if (roll.IsExceptionalSuccess && doorsOpened > 0)
        {
            await _lifecycle.ApplySocialConditionIfAbsentAsync(
                db,
                maneuver.InitiatorCharacterId,
                ConditionType.Inspired,
                "From an exceptional success while opening a Door in Social maneuvering.",
                actingUserId);
        }
    }
}
