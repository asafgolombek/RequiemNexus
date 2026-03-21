using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Implements <see cref="INpcCombatService"/>: manages NPC-specific combat state.
/// </summary>
public class NpcCombatService(
    ApplicationDbContext dbContext,
    ILogger<NpcCombatService> logger,
    IAuthorizationHelper authHelper,
    ISessionService sessionService,
    IDiceService diceService) : INpcCombatService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<NpcCombatService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISessionService _sessionService = sessionService;
    private readonly IDiceService _diceService = diceService;

    /// <inheritdoc />
    public async Task ApplyNpcDamageAsync(int entryId, char damageType, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        if (entry.CharacterId != null)
        {
            throw new InvalidOperationException("NPC damage tracking applies to NPC rows only.");
        }

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        string damage = entry.NpcHealthDamage ?? string.Empty;
        if (damage.Length >= entry.NpcHealthBoxes)
        {
            throw new InvalidOperationException("NPC health track is full.");
        }

        entry.NpcHealthDamage = damage + damageType;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "ST {UserId} applied damage '{Type}' to NPC '{NpcName}' in encounter {EncounterId}",
            storyTellerUserId,
            damageType,
            entry.NpcName,
            entry.EncounterId);

        await PublishInitiativeAsync(entry.EncounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task HealNpcDamageAsync(int entryId, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        if (entry.CharacterId != null)
        {
            throw new InvalidOperationException("NPC healing applies to NPC rows only.");
        }

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        string damage = entry.NpcHealthDamage ?? string.Empty;
        if (damage.Length > 0)
        {
            entry.NpcHealthDamage = damage[..^1];
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "ST {UserId} healed one box from NPC '{NpcName}' in encounter {EncounterId}",
                storyTellerUserId,
                entry.NpcName,
                entry.EncounterId);

            await PublishInitiativeAsync(entry.EncounterId, storyTellerUserId);
        }
    }

    /// <inheritdoc />
    public async Task SpendNpcWillpowerAsync(int entryId, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "spend NPC willpower");

        if (entry.NpcCurrentWillpower > 0)
        {
            entry.NpcCurrentWillpower--;
            await _dbContext.SaveChangesAsync();
            await PublishInitiativeAsync(entry.EncounterId, storyTellerUserId);
        }
    }

    /// <inheritdoc />
    public async Task RestoreNpcWillpowerAsync(int entryId, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "restore NPC willpower");

        if (entry.NpcCurrentWillpower < entry.NpcMaxWillpower)
        {
            entry.NpcCurrentWillpower++;
            await _dbContext.SaveChangesAsync();
            await PublishInitiativeAsync(entry.EncounterId, storyTellerUserId);
        }
    }

    /// <inheritdoc />
    public async Task SpendNpcVitaeAsync(int entryId, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        if (entry.NpcMaxVitae <= 0)
        {
            throw new InvalidOperationException("This NPC does not track Vitae.");
        }

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "spend NPC vitae");

        if (entry.NpcCurrentVitae > 0)
        {
            entry.NpcCurrentVitae--;
            await _dbContext.SaveChangesAsync();
            await PublishInitiativeAsync(entry.EncounterId, storyTellerUserId);
        }
    }

    /// <inheritdoc />
    public async Task RestoreNpcVitaeAsync(int entryId, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        if (entry.NpcMaxVitae <= 0)
        {
            throw new InvalidOperationException("This NPC does not track Vitae.");
        }

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "restore NPC vitae");

        if (entry.NpcCurrentVitae < entry.NpcMaxVitae)
        {
            entry.NpcCurrentVitae++;
            await _dbContext.SaveChangesAsync();
            await PublishInitiativeAsync(entry.EncounterId, storyTellerUserId);
        }
    }

    /// <inheritdoc />
    public async Task SetNpcEntryRevealAsync(int entryId, bool revealed, string? maskedDisplayName, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        if (entry.CharacterId != null)
        {
            throw new InvalidOperationException("Reveal toggle applies to NPC rows only.");
        }

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        entry.IsRevealed = revealed;
        if (maskedDisplayName != null)
        {
            entry.MaskedDisplayName = string.IsNullOrWhiteSpace(maskedDisplayName) ? null : maskedDisplayName.Trim();
        }

        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(entry.EncounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task<NpcEncounterRollResultDto> RollNpcEncounterPoolAsync(
        int initiativeEntryId,
        string? trait1,
        string? trait2,
        int? manualDicePool,
        string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == initiativeEntryId)
            ?? throw new InvalidOperationException($"Initiative entry {initiativeEntryId} not found.");

        if (entry.CharacterId != null)
        {
            throw new InvalidOperationException("NPC encounter rolls apply to NPC initiative rows only.");
        }

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "roll for an NPC in encounter");

        int pool;
        string poolDescription;

        if (manualDicePool.HasValue)
        {
            if (manualDicePool.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(manualDicePool), "Manual dice pool cannot be negative.");
            }

            pool = manualDicePool.Value;
            poolDescription = pool == 0
                ? "Manual pool (chance die)"
                : $"Manual pool ({pool})";
        }
        else
        {
            if (entry.ChronicleNpcId is not int chronicleNpcId)
            {
                throw new InvalidOperationException(
                    "Plain NPC rows require a manual dice pool. Link this NPC from Danse Macabre to roll by traits.");
            }

            if (string.IsNullOrWhiteSpace(trait1) || string.IsNullOrWhiteSpace(trait2))
            {
                throw new ArgumentException("Select two traits or enter a manual dice pool.");
            }

            string t1 = trait1.Trim();
            string t2 = trait2.Trim();
            if (!TryParseTraitKind(t1, out bool t1Attr) || !TryParseTraitKind(t2, out bool t2Attr))
            {
                throw new ArgumentException("Each trait must be a valid attribute or skill name.");
            }

            ChronicleNpc? npc = await _dbContext.ChronicleNpcs
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == chronicleNpcId);

            if (npc == null)
            {
                throw new InvalidOperationException($"Chronicle NPC {chronicleNpcId} not found.");
            }

            if (npc.CampaignId != encounter.CampaignId)
            {
                throw new InvalidOperationException("Chronicle NPC does not belong to this encounter's campaign.");
            }

            int v1 = ChronicleNpcTraitJsonReader.ReadTraitRating(t1, t1Attr, npc.AttributesJson, npc.SkillsJson);
            int v2 = ChronicleNpcTraitJsonReader.ReadTraitRating(t2, t2Attr, npc.AttributesJson, npc.SkillsJson);
            pool = v1 + v2;
            string d1 = TraitMetadata.GetDisplayName(t1);
            string d2 = TraitMetadata.GetDisplayName(t2);
            poolDescription = $"{d1} + {d2} ({pool})";
        }

        RollResult result = _diceService.Roll(pool, tenAgain: true);
        string npcLabel = entry.NpcName ?? "NPC";

        _logger.LogInformation(
            "NPC encounter roll for entry {EntryId} ({NpcName}) in encounter {EncounterId}: {PoolDescription}, successes={Successes}",
            initiativeEntryId,
            npcLabel,
            entry.EncounterId,
            poolDescription,
            result.Successes);

        return new NpcEncounterRollResultDto(
            result.Successes,
            result.DiceRolled,
            poolDescription,
            result.IsExceptionalSuccess,
            result.IsDramaticFailure);
    }

    private static bool TryParseTraitKind(string name, out bool isAttribute)
    {
        isAttribute = false;
        if (Enum.TryParse(name, out AttributeId _))
        {
            isAttribute = true;
            return true;
        }

        return Enum.TryParse(name, out SkillId _);
    }

    private async Task<CombatEncounter> LoadActiveCombatEncounterAsync(int encounterId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (!encounter.IsActive || encounter.IsDraft)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is not an active launched fight.");
        }

        if (encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        return encounter;
    }

    private async Task PublishInitiativeAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter? enc = await _dbContext.CombatEncounters
            .AsNoTracking()
            .Include(e => e.InitiativeEntries.OrderBy(i => i.Order))
                .ThenInclude(i => i.Character)
            .FirstOrDefaultAsync(e => e.Id == encounterId);

        if (enc == null || !enc.IsActive || enc.IsDraft)
        {
            return;
        }

        IReadOnlyList<InitiativeEntryDto> dtos = EncounterInitiativeBroadcastMapper.Map(enc);
        await _sessionService.UpdateInitiativeAsync(storyTellerUserId, enc.CampaignId, dtos);
    }
}
