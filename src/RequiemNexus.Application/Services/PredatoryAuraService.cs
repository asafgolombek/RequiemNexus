using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Observability;
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
/// Application orchestration for Predatory Aura Lash Out contests.
/// </summary>
public class PredatoryAuraService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    IDiceService diceService,
    IConditionRules conditionRules,
    RelationshipWebMetrics relationshipWebMetrics,
    ISessionService sessionService,
    ILogger<PredatoryAuraService> logger) : IPredatoryAuraService
{
    private const string _outcomeShaken = "Shaken";
    private const string _outcomeDraw = "Draw";

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IDiceService _diceService = diceService;
    private readonly IConditionRules _conditionRules = conditionRules;
    private readonly RelationshipWebMetrics _relationshipWebMetrics = relationshipWebMetrics;
    private readonly ISessionService _sessionService = sessionService;

    /// <inheritdoc />
    public async Task<Result<PredatoryAuraContestResultDto>> ResolveLashOutAsync(
        int chronicleId,
        int attackerCharacterId,
        int defenderCharacterId,
        string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);

        if (attackerCharacterId == defenderCharacterId)
        {
            return Result<PredatoryAuraContestResultDto>.Failure("A character cannot contest their own Predatory Aura.");
        }

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Character? attacker = await db.Characters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == attackerCharacterId);
        Character? defender = await db.Characters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == defenderCharacterId);

        if (attacker == null)
        {
            return Result<PredatoryAuraContestResultDto>.Failure($"Character {attackerCharacterId} was not found.");
        }

        if (defender == null)
        {
            return Result<PredatoryAuraContestResultDto>.Failure($"Character {defenderCharacterId} was not found.");
        }

        if (!attacker.CampaignId.HasValue || !defender.CampaignId.HasValue)
        {
            return Result<PredatoryAuraContestResultDto>.Failure("Both characters must belong to a chronicle.");
        }

        if (attacker.CampaignId.Value != chronicleId || defender.CampaignId.Value != chronicleId)
        {
            return Result<PredatoryAuraContestResultDto>.Failure(
                "Both characters must belong to the specified chronicle.");
        }

        bool isStoryteller = await db.Campaigns.AsNoTracking()
            .AnyAsync(c => c.Id == chronicleId && c.StoryTellerId == userId);

        if (!isStoryteller)
        {
            if (defender.ApplicationUserId == userId && attacker.ApplicationUserId != userId)
            {
                return Result<PredatoryAuraContestResultDto>.Failure(
                    "Only the attacking character's owner or the Storyteller may initiate a Predatory Aura contest.");
            }

            await _authHelper.RequireCharacterOwnerAsync(
                attackerCharacterId,
                userId,
                "lash out with Predatory Aura");
        }

        int attackerBp = attacker.BloodPotency;
        int defenderBp = defender.BloodPotency;

        RollResult attackerRoll = _diceService.Roll(attackerBp, tenAgain: true);
        RollResult defenderRoll = _diceService.Roll(defenderBp, tenAgain: true);

        PredatoryAuraOutcome outcome = PredatoryAuraRules.ResolveContest(
            attackerRoll.Successes,
            attackerBp,
            defenderRoll.Successes,
            defenderBp);

        int? winnerId = outcome switch
        {
            PredatoryAuraOutcome.AttackerWins => attackerCharacterId,
            PredatoryAuraOutcome.DefenderWins => defenderCharacterId,
            _ => null,
        };

        string outcomeApplied = outcome == PredatoryAuraOutcome.Draw ? _outcomeDraw : _outcomeShaken;
        int? loserId = outcome switch
        {
            PredatoryAuraOutcome.AttackerWins => defenderCharacterId,
            PredatoryAuraOutcome.DefenderWins => attackerCharacterId,
            _ => null,
        };

        var contest = new PredatoryAuraContest
        {
            ChronicleId = chronicleId,
            AttackerCharacterId = attackerCharacterId,
            DefenderCharacterId = defenderCharacterId,
            AttackerBloodPotency = attackerBp,
            DefenderBloodPotency = defenderBp,
            AttackerSuccesses = attackerRoll.Successes,
            DefenderSuccesses = defenderRoll.Successes,
            WinnerId = winnerId,
            OutcomeApplied = outcomeApplied,
            ResolvedAt = DateTime.UtcNow,
            IsLashOut = true,
        };

        db.PredatoryAuraContests.Add(contest);
        await db.SaveChangesAsync();
        int contestId = contest.Id;

        if (loserId.HasValue)
        {
            string description = _conditionRules.GetConditionDescription(ConditionType.Shaken);
            var condition = new CharacterCondition
            {
                CharacterId = loserId.Value,
                ConditionType = ConditionType.Shaken,
                Description = description,
                AppliedAt = DateTime.UtcNow,
                AwardsBeat = _conditionRules.AwardsBeatOnResolve(ConditionType.Shaken),
                AppliedByUserId = userId,
                SourceTag = ContestSourceTag(contestId),
            };

            db.CharacterConditions.Add(condition);
            await db.SaveChangesAsync();
            await NotifyConditionToastAsync(db, loserId.Value, ConditionType.Shaken, isRemoval: false);
        }

        string attackerName = attacker.Name ?? "?";
        string defenderName = defender.Name ?? "?";

        var dto = new PredatoryAuraContestResultDto(
            contestId,
            chronicleId,
            attackerCharacterId,
            attackerName,
            defenderCharacterId,
            defenderName,
            attackerBp,
            defenderBp,
            attackerRoll.Successes,
            defenderRoll.Successes,
            outcome,
            winnerId,
            outcomeApplied,
            loserId.HasValue ? _outcomeShaken : null);

        string summary = BuildBroadcastSummary(attackerName, defenderName, outcome);
        await _sessionService.BroadcastCharacterUpdateAsync(attackerCharacterId);
        await _sessionService.BroadcastCharacterUpdateAsync(defenderCharacterId);
        await _sessionService.BroadcastRelationshipUpdateAsync(
            chronicleId,
            new RelationshipUpdateDto(RelationshipUpdateType.PredatoryAura, loserId, summary));

        _relationshipWebMetrics.RecordPredatoryAuraContestResolved();
        logger.LogInformation(
            "Predatory Aura contest {ContestId} chronicle {ChronicleId} outcome {Outcome} attacker {AttackerId} defender {DefenderId} {CorrelationId}",
            contestId,
            chronicleId,
            outcome,
            attackerCharacterId,
            defenderCharacterId,
            correlationId);

        return Result<PredatoryAuraContestResultDto>.Success(dto);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PredatoryAuraContestSummaryDto>> GetRecentContestsAsync(
        int chronicleId,
        string userId,
        int take = 25)
    {
        await _authHelper.RequireStorytellerAsync(chronicleId, userId, "view Predatory Aura history");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        int limit = Math.Clamp(take, 1, 100);
        List<PredatoryAuraContest> rows = await db.PredatoryAuraContests.AsNoTracking()
            .Include(c => c.AttackerCharacter)
            .Include(c => c.DefenderCharacter)
            .Where(c => c.ChronicleId == chronicleId)
            .OrderByDescending(c => c.ResolvedAt)
            .Take(limit)
            .ToListAsync();

        return rows.ConvertAll(c => new PredatoryAuraContestSummaryDto(
            c.Id,
            c.AttackerCharacterId,
            c.DefenderCharacterId,
            c.AttackerCharacter?.Name ?? "?",
            c.DefenderCharacter?.Name ?? "?",
            c.AttackerSuccesses,
            c.DefenderSuccesses,
            c.WinnerId,
            c.OutcomeApplied,
            c.ResolvedAt));
    }

    private static string ContestSourceTag(int contestId) => $"predatoryaura:{contestId}";

    private static string BuildBroadcastSummary(
        string attackerName,
        string defenderName,
        PredatoryAuraOutcome outcome)
    {
        return outcome switch
        {
            PredatoryAuraOutcome.Draw =>
                $"Predatory Aura: {attackerName} vs {defenderName} — true draw (no effect).",
            _ =>
                $"Predatory Aura: {attackerName} lashed out at {defenderName} — loser gains Shaken.",
        };
    }

    private async Task NotifyConditionToastAsync(
        ApplicationDbContext db,
        int characterId,
        ConditionType type,
        bool isRemoval)
    {
        string? ownerId = await db.Characters.AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => c.ApplicationUserId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(ownerId))
        {
            return;
        }

        await _sessionService.NotifyConditionToastAsync(
            ownerId,
            new ConditionNotificationDto(characterId, type.ToString(), IsTilt: false, isRemoval));
    }

    private IDisposable BeginCorrelationScope(string correlationId) =>
        logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })
        ?? NoOpDisposable.Instance;
}
