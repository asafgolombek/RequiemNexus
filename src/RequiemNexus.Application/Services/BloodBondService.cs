using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Observability;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

#pragma warning disable SA1201 // Order favors cohesive private helpers grouped with Blood Bond operations
#pragma warning disable SA1202
#pragma warning disable SA1204

/// <summary>
/// Application orchestration for Blood Bonds: feeding, fading, Conditions with <c>SourceTag</c> isolation, and alerts.
/// </summary>
public class BloodBondService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    IConditionRules conditionRules,
    IBeatLedgerService beatLedger,
    ICharacterCreationRules creationRules,
    RelationshipWebMetrics relationshipWebMetrics,
    ISessionService sessionService,
    ILogger<BloodBondService> logger) : IBloodBondService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IConditionRules _conditionRules = conditionRules;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly ICharacterCreationRules _creationRules = creationRules;
    private readonly RelationshipWebMetrics _relationshipWebMetrics = relationshipWebMetrics;
    private readonly ISessionService _sessionService = sessionService;

    /// <inheritdoc />
    public async Task<Result<BloodBondDto>> RecordFeedingAsync(RecordFeedingRequest request, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Result<(string Key, int? CharId, int? NpcId, string? Display)> regnant =
            ValidateRegnantSelection(request);
        if (!regnant.IsSuccess)
        {
            return Result<BloodBondDto>.Failure(regnant.Error!);
        }

        (string regnantKey, int? regnantCharacterId, int? regnantNpcId, string? regnantDisplayName) = regnant.Value;

        Character? thrall = await db.Characters.FirstOrDefaultAsync(c => c.Id == request.ThrallCharacterId);
        if (thrall == null)
        {
            return Result<BloodBondDto>.Failure($"Character {request.ThrallCharacterId} was not found.");
        }

        if (thrall.CampaignId != request.ChronicleId)
        {
            return Result<BloodBondDto>.Failure("The thrall must belong to the specified chronicle.");
        }

        await _authHelper.RequireStorytellerAsync(request.ChronicleId, userId, "record Blood Bond feeding");

        if (regnantCharacterId.HasValue && regnantCharacterId.Value == request.ThrallCharacterId)
        {
            return Result<BloodBondDto>.Failure("A character cannot form a Blood Bond to themselves as regnant.");
        }

        if (regnantCharacterId.HasValue)
        {
            Character? regnantPc = await db.Characters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == regnantCharacterId.Value);
            if (regnantPc == null)
            {
                return Result<BloodBondDto>.Failure($"Regnant character {regnantCharacterId.Value} was not found.");
            }

            if (regnantPc.CampaignId != request.ChronicleId)
            {
                return Result<BloodBondDto>.Failure("The regnant PC must belong to the same chronicle.");
            }
        }

        if (regnantNpcId.HasValue)
        {
            ChronicleNpc? npc = await db.ChronicleNpcs.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == regnantNpcId.Value);
            if (npc == null)
            {
                return Result<BloodBondDto>.Failure($"Chronicle NPC {regnantNpcId.Value} was not found.");
            }

            if (npc.CampaignId != request.ChronicleId)
            {
                return Result<BloodBondDto>.Failure("The regnant NPC must belong to the same chronicle.");
            }
        }

        BloodBond? existing = await db.BloodBonds.FirstOrDefaultAsync(b =>
            b.ChronicleId == request.ChronicleId
            && b.ThrallCharacterId == request.ThrallCharacterId
            && b.RegnantKey == regnantKey);

        DateTime utcNow = DateTime.UtcNow;

        if (existing == null)
        {
            var bond = new BloodBond
            {
                ChronicleId = request.ChronicleId,
                ThrallCharacterId = request.ThrallCharacterId,
                RegnantCharacterId = regnantCharacterId,
                RegnantNpcId = regnantNpcId,
                RegnantDisplayName = regnantDisplayName,
                RegnantKey = regnantKey,
                Stage = 1,
                LastFedAt = utcNow,
                CreatedAt = utcNow,
                Notes = request.Notes,
            };
            db.BloodBonds.Add(bond);
            await db.SaveChangesAsync();

            await ApplyBondConditionAsync(db, bond.Id, request.ThrallCharacterId, ConditionType.Addicted, userId);
            await db.SaveChangesAsync();

            string regnantLabel = await RegnantLabelAsync(db, bond);
            await PushUpdatesAsync(
                request.ChronicleId,
                request.ThrallCharacterId,
                $"Blood Bond established at Stage 1 with {regnantLabel}.");
            _relationshipWebMetrics.RecordBloodBondStageChange(1);
            logger.LogInformation(
                "Blood Bond created at Stage 1 for thrall {ThrallId} in chronicle {ChronicleId} {CorrelationId}",
                request.ThrallCharacterId,
                request.ChronicleId,
                correlationId);

            return Result<BloodBondDto>.Success(await MapToDtoAsync(db, bond.Id, utcNow));
        }

        if (existing.Stage >= 3)
        {
            existing.LastFedAt = utcNow;
            if (request.Notes != null)
            {
                existing.Notes = request.Notes;
            }

            await db.SaveChangesAsync();
            logger.LogInformation(
                "Blood Bond {BondId} Stage 3 feeding refresh (LastFedAt only) {CorrelationId}",
                existing.Id,
                correlationId);
            return Result<BloodBondDto>.Success(await MapToDtoAsync(db, existing.Id, utcNow));
        }

        int priorStage = existing.Stage;
        ConditionType priorCondition = BloodBondRules.ConditionForStage(priorStage);
        await ResolveBondConditionsAsync(db, existing.Id, request.ThrallCharacterId, priorCondition, userId);

        existing.Stage = priorStage + 1;
        existing.LastFedAt = utcNow;
        if (request.Notes != null)
        {
            existing.Notes = request.Notes;
        }

        await db.SaveChangesAsync();

        ConditionType nextCondition = BloodBondRules.ConditionForStage(existing.Stage);
        await ApplyBondConditionAsync(db, existing.Id, request.ThrallCharacterId, nextCondition, userId);
        await db.SaveChangesAsync();

        string escalateLabel = await RegnantLabelAsync(db, existing);
        await PushUpdatesAsync(
            request.ChronicleId,
            request.ThrallCharacterId,
            $"Blood Bond advanced from Stage {priorStage} to {existing.Stage} ({escalateLabel}).");

        _relationshipWebMetrics.RecordBloodBondStageChange(existing.Stage);
        logger.LogInformation(
            "Blood Bond {BondId} escalated to Stage {Stage} {CorrelationId}",
            existing.Id,
            existing.Stage,
            correlationId);

        return Result<BloodBondDto>.Success(await MapToDtoAsync(db, existing.Id, utcNow));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BloodBondDto>> GetBondsForThrallAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "view Blood Bonds");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        List<int> ids = await db.BloodBonds.AsNoTracking()
            .Where(b => b.ThrallCharacterId == characterId)
            .Select(b => b.Id)
            .ToListAsync();

        List<BloodBondDto> list = [];
        foreach (int id in ids)
        {
            list.Add(await MapToDtoAsync(db, id, now));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BloodBondDto>> GetBondsInChronicleAsync(int chronicleId, string userId)
    {
        await _authHelper.RequireStorytellerAsync(chronicleId, userId, "list Blood Bonds");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        List<int> ids = await db.BloodBonds.AsNoTracking()
            .Where(b => b.ChronicleId == chronicleId)
            .OrderBy(b => b.ThrallCharacterId)
            .ThenBy(b => b.RegnantKey)
            .Select(b => b.Id)
            .ToListAsync();

        List<BloodBondDto> list = [];
        foreach (int id in ids)
        {
            list.Add(await MapToDtoAsync(db, id, now));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> FadeBondAsync(int bondId, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        BloodBond? bond = await db.BloodBonds.FirstOrDefaultAsync(b => b.Id == bondId);
        if (bond == null)
        {
            return Result<Unit>.Failure($"Blood Bond {bondId} was not found.");
        }

        await _authHelper.RequireStorytellerAsync(bond.ChronicleId, userId, "fade Blood Bond");

        int thrallId = bond.ThrallCharacterId;
        int stage = bond.Stage;
        ConditionType currentType = BloodBondRules.ConditionForStage(stage);
        await ResolveBondConditionsAsync(db, bond.Id, thrallId, currentType, userId);

        if (stage <= 1)
        {
            string removedLabel = await RegnantLabelAsync(db, bond);
            db.BloodBonds.Remove(bond);
            await db.SaveChangesAsync();
            await PushUpdatesAsync(bond.ChronicleId, thrallId, $"Blood Bond removed ({removedLabel}).");
            _relationshipWebMetrics.RecordBloodBondStageChange(0);
            logger.LogInformation("Blood Bond {BondId} removed at Stage 1 fade {CorrelationId}", bondId, correlationId);
            return Result<Unit>.Success(Unit.Value);
        }

        bond.Stage = stage - 1;
        await db.SaveChangesAsync();

        ConditionType newType = BloodBondRules.ConditionForStage(bond.Stage);
        await ApplyBondConditionAsync(db, bond.Id, thrallId, newType, userId);
        await db.SaveChangesAsync();

        string fadedLabel = await RegnantLabelAsync(db, bond);
        await PushUpdatesAsync(
            bond.ChronicleId,
            thrallId,
            $"Blood Bond faded from Stage {stage} to {bond.Stage} ({fadedLabel}).");

        _relationshipWebMetrics.RecordBloodBondStageChange(bond.Stage);
        logger.LogInformation(
            "Blood Bond {BondId} faded to Stage {Stage} {CorrelationId}",
            bondId,
            bond.Stage,
            correlationId);

        return Result<Unit>.Success(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BloodBondDto>> GetFadingAlertsAsync(int chronicleId, string userId)
    {
        await _authHelper.RequireStorytellerAsync(chronicleId, userId, "view Blood Bond fading alerts");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        List<BloodBondDto> all = [];
        List<int> ids = await db.BloodBonds.AsNoTracking()
            .Where(b => b.ChronicleId == chronicleId)
            .Select(b => b.Id)
            .ToListAsync();

        foreach (int id in ids)
        {
            BloodBondDto dto = await MapToDtoAsync(db, id, now);
            if (dto.IsFading)
            {
                all.Add(dto);
            }
        }

        return all;
    }

    private static string BondSourceTag(int bondId) => $"bloodbond:{bondId}";

    private static Result<(string Key, int? CharId, int? NpcId, string? Display)> ValidateRegnantSelection(
        RecordFeedingRequest request)
    {
        int flags = (request.RegnantCharacterId.HasValue ? 1 : 0)
                  + (request.RegnantNpcId.HasValue ? 1 : 0)
                  + (!string.IsNullOrWhiteSpace(request.RegnantDisplayName) ? 1 : 0);

        if (flags != 1)
        {
            return Result<(string, int?, int?, string?)>.Failure(
                "Specify exactly one regnant: a PC character, an NPC, or a display name.");
        }

        if (request.RegnantCharacterId.HasValue)
        {
            return Result<(string, int?, int?, string?)>.Success((
                BloodBondRegnantKey.ForCharacter(request.RegnantCharacterId.Value),
                request.RegnantCharacterId.Value,
                null,
                null));
        }

        if (request.RegnantNpcId.HasValue)
        {
            return Result<(string, int?, int?, string?)>.Success((
                BloodBondRegnantKey.ForNpc(request.RegnantNpcId.Value),
                null,
                request.RegnantNpcId.Value,
                null));
        }

        string trimmed = request.RegnantDisplayName!.Trim();
        return Result<(string, int?, int?, string?)>.Success((
            BloodBondRegnantKey.ForDisplayName(trimmed),
            null,
            null,
            trimmed));
    }

    private async Task ApplyBondConditionAsync(
        ApplicationDbContext db,
        int bondId,
        int thrallCharacterId,
        ConditionType type,
        string appliedByUserId)
    {
        string tag = BondSourceTag(bondId);
        string description = _conditionRules.GetConditionDescription(type);
        bool awardsBeat = type switch
        {
            ConditionType.Bound => true,
            _ => false,
        };

        var condition = new CharacterCondition
        {
            CharacterId = thrallCharacterId,
            ConditionType = type,
            Description = description,
            AppliedAt = DateTime.UtcNow,
            AwardsBeat = awardsBeat,
            AppliedByUserId = appliedByUserId,
            SourceTag = tag,
        };

        db.CharacterConditions.Add(condition);
        await NotifyConditionToastAsync(db, thrallCharacterId, type, isRemoval: false);
    }

    private async Task ResolveBondConditionsAsync(
        ApplicationDbContext db,
        int bondId,
        int thrallCharacterId,
        ConditionType type,
        string resolvedByUserId)
    {
        string tag = BondSourceTag(bondId);
        List<CharacterCondition> rows = await db.CharacterConditions
            .Include(c => c.Character)
            .Where(c => c.CharacterId == thrallCharacterId
                        && c.ConditionType == type
                        && !c.IsResolved
                        && c.SourceTag == tag)
            .ToListAsync();

        foreach (CharacterCondition condition in rows)
        {
            condition.IsResolved = true;
            condition.ResolvedAt = DateTime.UtcNow;

            if (condition.AwardsBeat)
            {
                await _beatLedger.RecordBeatAsync(
                    condition.CharacterId,
                    condition.Character?.CampaignId,
                    BeatSource.ConditionResolved,
                    $"Resolved Condition: {condition.CustomName ?? condition.ConditionType.ToString()}",
                    resolvedByUserId);

                Character character = await db.Characters.FindAsync(condition.CharacterId)
                    ?? throw new InvalidOperationException($"Character {condition.CharacterId} not found.");

                character.Beats++;

                if (_creationRules.TryConvertBeats(character.Beats, out int newBeats, out int xpGained))
                {
                    character.Beats = newBeats;
                    character.ExperiencePoints += xpGained;
                    character.TotalExperiencePoints += xpGained;

                    await _beatLedger.RecordXpCreditAsync(
                        character.Id,
                        character.CampaignId,
                        xpGained,
                        XpSource.BeatConversion,
                        $"Converted 5 Beats to {xpGained} XP",
                        null);
                }
            }

            await NotifyConditionToastAsync(db, thrallCharacterId, type, isRemoval: true);
        }
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

    private async Task PushUpdatesAsync(int chronicleId, int thrallCharacterId, string summary)
    {
        await _sessionService.BroadcastCharacterUpdateAsync(thrallCharacterId);
        await _sessionService.BroadcastRelationshipUpdateAsync(
            chronicleId,
            new RelationshipUpdateDto(RelationshipUpdateType.BloodBond, thrallCharacterId, summary));
    }

    private async Task<BloodBondDto> MapToDtoAsync(ApplicationDbContext db, int bondId, DateTime now)
    {
        BloodBond bond = await db.BloodBonds
            .AsNoTracking()
            .Include(b => b.ThrallCharacter)
            .Include(b => b.RegnantCharacter)
            .Include(b => b.RegnantNpc)
            .FirstAsync(b => b.Id == bondId);

        string regnantLabel = ResolveRegnantLabel(bond);
        string activeName = _conditionRules.GetConditionDescription(BloodBondRules.ConditionForStage(bond.Stage));

        return new BloodBondDto(
            bond.Id,
            bond.ChronicleId,
            bond.ThrallCharacterId,
            bond.ThrallCharacter?.Name ?? "?",
            bond.RegnantCharacterId,
            bond.RegnantNpcId,
            regnantLabel,
            bond.Stage,
            bond.LastFedAt,
            BloodBondRules.IsFading(bond.LastFedAt, now),
            activeName);
    }

    private static string ResolveRegnantLabel(BloodBond bond) =>
        bond.RegnantCharacter?.Name
        ?? bond.RegnantNpc?.Name
        ?? bond.RegnantDisplayName
        ?? string.Empty;

    private async Task<string> RegnantLabelAsync(ApplicationDbContext db, BloodBond bond)
    {
        if (bond.RegnantCharacterId.HasValue)
        {
            string? n = await db.Characters.AsNoTracking()
                .Where(c => c.Id == bond.RegnantCharacterId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(n))
            {
                return n;
            }
        }

        if (bond.RegnantNpcId.HasValue)
        {
            string? n = await db.ChronicleNpcs.AsNoTracking()
                .Where(x => x.Id == bond.RegnantNpcId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(n))
            {
                return n;
            }
        }

        return string.IsNullOrEmpty(bond.RegnantDisplayName) ? "?" : bond.RegnantDisplayName;
    }

    private IDisposable BeginCorrelationScope(string correlationId) =>
        logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })
            ?? NoOpDisposable.Instance;
}
