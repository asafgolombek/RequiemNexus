using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Manages participant entries in a combat encounter: adding and removing characters and NPCs.
/// Encounter lifecycle is handled by <see cref="EncounterService"/>.
/// </summary>
public class EncounterParticipantService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ISessionService sessionService,
    IPredatoryAuraService predatoryAuraService) : IEncounterParticipantService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISessionService _sessionService = sessionService;
    private readonly IPredatoryAuraService _predatoryAuraService = predatoryAuraService;

    /// <inheritdoc />
    public async Task BulkAddOnlinePlayersAsync(int encounterId, IReadOnlyList<int> characterIds, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadMutableEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        foreach (int charId in characterIds)
        {
            bool exists = await _dbContext.InitiativeEntries
                .AnyAsync(i => i.EncounterId == encounterId && i.CharacterId == charId);

            if (exists)
            {
                continue;
            }

            Character character = await _dbContext.Characters
                .AsNoTracking()
                .Include(c => c.Attributes)
                .FirstOrDefaultAsync(c => c.Id == charId)
                ?? throw new InvalidOperationException($"Character {charId} not found.");

            int wits = character.GetAttributeRating(AttributeId.Wits);
            int composure = character.GetAttributeRating(AttributeId.Composure);
            int mod = wits + composure;
            int roll = Random.Shared.Next(1, 11);

            await AddCharacterToEncounterCoreAsync(encounterId, charId, mod, roll);
            await _dbContext.SaveChangesAsync();
            await TriggerPassivePredatoryAuraForNewParticipantIfNeededAsync(encounter, charId, storyTellerUserId);
        }

        await RecalculateOrderAsync(encounterId);
        await PublishInitiativeIfActiveAsync(encounter, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task<InitiativeEntry> AddCharacterToEncounterAsync(
        int encounterId,
        int characterId,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadMutableEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        InitiativeEntry entry = await AddCharacterToEncounterCoreAsync(encounterId, characterId, initiativeMod, rollResult);
        await _dbContext.SaveChangesAsync();
        await TriggerPassivePredatoryAuraForNewParticipantIfNeededAsync(encounter, characterId, storyTellerUserId);
        await RecalculateOrderAsync(encounterId);
        await PublishInitiativeIfActiveAsync(encounter, storyTellerUserId);

        return entry;
    }

    /// <inheritdoc />
    public async Task<InitiativeEntry> AddNpcToEncounterAsync(
        int encounterId,
        string npcName,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId,
        int npcHealthBoxes = 7,
        int npcMaxWillpower = 4,
        int? chronicleNpcId = null,
        int npcMaxVitae = 0)
    {
        CombatEncounter encounter = await LoadMutableEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        string name = (npcName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("NPC name is required.", nameof(npcName));
        }

        InitiativeEntry entry = new()
        {
            EncounterId = encounterId,
            ChronicleNpcId = chronicleNpcId,
            NpcName = name,
            InitiativeMod = initiativeMod,
            RollResult = rollResult,
            Total = initiativeMod + rollResult,
            HasActed = false,
            IsHeld = false,
            IsRevealed = true,
            NpcHealthBoxes = Math.Clamp(npcHealthBoxes, 1, 50),
            NpcHealthDamage = string.Empty,
            NpcMaxWillpower = Math.Clamp(npcMaxWillpower, 1, 20),
            NpcCurrentWillpower = Math.Clamp(npcMaxWillpower, 1, 20),
            NpcMaxVitae = Math.Clamp(npcMaxVitae, 0, 100),
            NpcCurrentVitae = Math.Clamp(npcMaxVitae, 0, 100),
        };

        _dbContext.InitiativeEntries.Add(entry);
        await _dbContext.SaveChangesAsync();
        await RecalculateOrderAsync(encounterId);
        await PublishInitiativeIfActiveAsync(encounter, storyTellerUserId);

        return entry;
    }

    /// <inheritdoc />
    public async Task<InitiativeEntry> AddNpcToEncounterFromChronicleNpcAsync(
        int encounterId,
        int chronicleNpcId,
        int initiativeMod,
        int rollResult,
        int healthBoxes,
        int maxWillpower,
        int maxVitae,
        string storyTellerUserId)
    {
        bool exists = await _dbContext.InitiativeEntries
            .AnyAsync(i => i.EncounterId == encounterId && i.ChronicleNpcId == chronicleNpcId);

        if (exists)
        {
            throw new InvalidOperationException("This chronicle NPC is already in the encounter.");
        }

        ChronicleNpc? npc = await _dbContext.ChronicleNpcs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == chronicleNpcId)
            ?? throw new InvalidOperationException($"Chronicle NPC {chronicleNpcId} not found.");

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);

        if (encounter.CampaignId != npc.CampaignId)
        {
            throw new InvalidOperationException("Chronicle NPC does not belong to this encounter's campaign.");
        }

        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        return await AddNpcToEncounterAsync(
            encounterId,
            npc.Name,
            initiativeMod,
            rollResult,
            storyTellerUserId,
            healthBoxes,
            maxWillpower,
            chronicleNpcId,
            maxVitae);
    }

    /// <inheritdoc />
    public async Task RemoveEntryAsync(int entryId, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        CombatEncounter encounter = await LoadMutableEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        _dbContext.InitiativeEntries.Remove(entry);
        await _dbContext.SaveChangesAsync();
        await RecalculateOrderAsync(entry.EncounterId);
        await PublishInitiativeIfActiveAsync(encounter, storyTellerUserId);
    }

    private async Task<InitiativeEntry> AddCharacterToEncounterCoreAsync(int encounterId, int characterId, int initiativeMod, int rollResult)
    {
        InitiativeEntry entry = new() { EncounterId = encounterId, CharacterId = characterId, InitiativeMod = initiativeMod, RollResult = rollResult, Total = initiativeMod + rollResult, HasActed = false, IsHeld = false, IsRevealed = true };
        _dbContext.InitiativeEntries.Add(entry);
        return await Task.FromResult(entry);
    }

    /// <summary>
    /// When a Kindred joins an encounter, resolves passive Predatory Aura against each other Kindred already in the encounter (Phase 18).
    /// </summary>
    private async Task TriggerPassivePredatoryAuraForNewParticipantIfNeededAsync(
        CombatEncounter encounter,
        int newCharacterId,
        string storyTellerUserId)
    {
        bool isVampire = await _dbContext.Characters.AsNoTracking()
            .AnyAsync(c => c.Id == newCharacterId && c.CreatureType == CreatureType.Vampire);

        if (!isVampire)
        {
            return;
        }

        List<int> peerIds = await _dbContext.InitiativeEntries
            .AsNoTracking()
            .Where(i => i.EncounterId == encounter.Id && i.CharacterId != null && i.CharacterId != newCharacterId)
            .Select(i => i.CharacterId!.Value)
            .Distinct()
            .ToListAsync();

        if (peerIds.Count == 0)
        {
            return;
        }

        List<int> vampirePeerIds = await _dbContext.Characters
            .AsNoTracking()
            .Where(c => peerIds.Contains(c.Id) && c.CreatureType == CreatureType.Vampire)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (int otherId in vampirePeerIds)
        {
            _ = await _predatoryAuraService.ResolvePassiveContestAsync(
                encounter.CampaignId,
                newCharacterId,
                otherId,
                storyTellerUserId,
                encounter.Id);
        }
    }

    private async Task<CombatEncounter> LoadMutableEncounterAsync(int encounterId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters.FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");
        if (encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        return encounter;
    }

    private async Task<CombatEncounter> LoadActiveCombatEncounterAsync(int encounterId)
    {
        CombatEncounter encounter = await LoadMutableEncounterAsync(encounterId);
        if (!encounter.IsActive || encounter.IsDraft)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is not an active launched fight.");
        }

        return encounter;
    }

    private async Task RecalculateOrderAsync(int encounterId)
    {
        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries.Where(i => i.EncounterId == encounterId).ToListAsync();
        static int Tier(InitiativeEntry e) => e.HasActed ? 2 : (e.IsHeld ? 1 : 0);
        List<InitiativeEntry> sorted = entries.OrderBy(Tier).ThenByDescending(i => i.Total).ThenByDescending(i => i.InitiativeMod).ThenBy(i => i.CharacterId == null ? 1 : 0).ToList();
        int order = 1;
        foreach (var entry in sorted)
        {
            entry.Order = order++;
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task PublishInitiativeIfActiveAsync(CombatEncounter encounter, string storyTellerUserId)
    {
        if (encounter.IsActive && !encounter.IsDraft)
        {
            await PublishInitiativeAsync(encounter.Id, storyTellerUserId);
        }
    }

    private async Task PublishInitiativeAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter? enc = await _dbContext.CombatEncounters
            .AsNoTracking()
            .Include(e => e.InitiativeEntries.OrderBy(i => i.Order)).ThenInclude(i => i.Character)
            .FirstOrDefaultAsync(e => e.Id == encounterId);

        if (enc == null || !enc.IsActive || enc.IsDraft)
        {
            return;
        }

        await _sessionService.UpdateInitiativeAsync(storyTellerUserId, enc.CampaignId, EncounterInitiativeBroadcastMapper.Map(enc));
    }
}
