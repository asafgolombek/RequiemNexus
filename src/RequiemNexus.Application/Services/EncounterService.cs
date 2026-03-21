using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Implements <see cref="IEncounterService"/>: focused on active encounter execution and initiative.
/// </summary>
public class EncounterService(
    ApplicationDbContext dbContext,
    ILogger<EncounterService> logger,
    IAuthorizationHelper authHelper,
    ISessionService sessionService) : IEncounterService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<EncounterService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISessionService _sessionService = sessionService;

    /// <inheritdoc />
    public async Task LaunchEncounterAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .Include(e => e.NpcTemplates)
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (!encounter.IsDraft)
        {
            throw new InvalidOperationException("Only draft encounters can be launched.");
        }

        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        await EnsureNoOtherOpenEncounterAsync(encounter.CampaignId, exceptEncounterId: encounterId);

        foreach (EncounterNpcTemplate template in encounter.NpcTemplates)
        {
            int roll = Random.Shared.Next(1, 11);
            InitiativeEntry entry = new()
            {
                EncounterId = encounterId,
                ChronicleNpcId = template.ChronicleNpcId,
                NpcName = template.Name,
                InitiativeMod = template.InitiativeMod,
                RollResult = roll,
                Total = template.InitiativeMod + roll,
                HasActed = false,
                IsHeld = false,
                IsRevealed = template.IsRevealed,
                MaskedDisplayName = template.MaskedDisplayName,
                NpcHealthBoxes = template.HealthBoxes,
                NpcHealthDamage = string.Empty,
                NpcMaxWillpower = template.MaxWillpower,
                NpcCurrentWillpower = template.MaxWillpower,
                NpcMaxVitae = template.MaxVitae,
                NpcCurrentVitae = template.MaxVitae,
            };
            _dbContext.InitiativeEntries.Add(entry);
        }

        encounter.IsDraft = false;
        encounter.IsActive = true;
        encounter.IsPaused = false;
        encounter.CurrentRound = 1;

        await _dbContext.SaveChangesAsync();
        await RecalculateOrderAsync(encounterId);
        await PublishInitiativeAsync(encounterId, storyTellerUserId);

        _logger.LogInformation(
            "Encounter {EncounterId} launched in campaign {CampaignId} by ST {UserId}",
            encounterId,
            encounter.CampaignId,
            storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task BulkAddOnlinePlayersAsync(int encounterId, IReadOnlyList<int> characterIds, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadMutableEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        foreach (int charId in characterIds)
        {
            // Verify character is not already in the encounter
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

            int wits = character.GetAttributeRating(Domain.AttributeId.Wits);
            int composure = character.GetAttributeRating(Domain.AttributeId.Composure);
            int mod = wits + composure;
            int roll = Random.Shared.Next(1, 11);

            await AddCharacterToEncounterCoreAsync(encounterId, charId, mod, roll);
        }

        await _dbContext.SaveChangesAsync();
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
    public async Task AdvanceTurnAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .OrderBy(i => i.Order)
            .ToListAsync();

        InitiativeEntry? currentActor = entries.FirstOrDefault(e => !e.HasActed);
        if (currentActor != null)
        {
            currentActor.HasActed = true;
            currentActor.IsHeld = false; // Hold expires when you finally act
        }

        if (entries.All(e => e.HasActed))
        {
            foreach (var e in entries)
            {
                e.HasActed = false;
                e.IsHeld = false;
            }

            encounter.CurrentRound++;
        }

        await _dbContext.SaveChangesAsync();
        await RecalculateOrderAsync(encounterId);
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task ResolveEncounterAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadMutableEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        encounter.IsActive = false;
        encounter.IsPaused = false;
        encounter.ResolvedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await _sessionService.UpdateInitiativeAsync(storyTellerUserId, encounter.CampaignId, []);

        _logger.LogInformation("Encounter {EncounterId} resolved by ST {UserId}", encounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task PauseEncounterAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        encounter.IsActive = false;
        encounter.IsPaused = true;

        await _dbContext.SaveChangesAsync();
        await _sessionService.UpdateInitiativeAsync(storyTellerUserId, encounter.CampaignId, []);
    }

    /// <inheritdoc />
    public async Task ResumeEncounterAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (!encounter.IsPaused)
        {
            throw new InvalidOperationException("Only paused encounters can be resumed.");
        }

        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");
        await EnsureNoOtherOpenEncounterAsync(encounter.CampaignId, exceptEncounterId: encounterId);

        encounter.IsActive = true;
        encounter.IsPaused = false;

        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task<List<CombatEncounter>> GetEncountersAsync(int campaignId, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "view encounters");

        return await _dbContext.CombatEncounters
            .Include(e => e.InitiativeEntries.OrderBy(i => i.Order))
            .Include(e => e.NpcTemplates)
            .Where(e => e.CampaignId == campaignId)
            .OrderByDescending(e => e.IsActive)
            .ThenByDescending(e => e.IsPaused)
            .ThenByDescending(e => e.IsDraft)
            .ThenByDescending(e => e.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CombatEncounter?> GetEncounterAsync(int encounterId, string userId)
    {
        CombatEncounter? encounter = await _dbContext.CombatEncounters
            .Include(e => e.InitiativeEntries.OrderBy(i => i.Order))
                .ThenInclude(i => i.Character)
            .Include(e => e.NpcTemplates)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == encounterId);

        if (encounter == null)
        {
            return null;
        }

        await _authHelper.RequireCampaignMemberAsync(encounter.CampaignId, userId, "view encounter");

        bool isSt = await _dbContext.Campaigns.AnyAsync(c => c.Id == encounter.CampaignId && c.StoryTellerId == userId);
        return isSt ? encounter : RedactEncounterForPlayer(encounter, userId);
    }

    /// <inheritdoc />
    public async Task<CombatEncounter?> GetActiveEncounterForCampaignAsync(int campaignId, string userId)
    {
        await _authHelper.RequireCampaignMemberAsync(campaignId, userId, "view active encounter");

        CombatEncounter? encounter = await _dbContext.CombatEncounters
            .Include(e => e.InitiativeEntries.OrderBy(i => i.Order))
                .ThenInclude(i => i.Character)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CampaignId == campaignId && e.IsActive && !e.IsDraft);

        if (encounter == null)
        {
            return null;
        }

        bool isSt = await _dbContext.Campaigns.AnyAsync(c => c.Id == campaignId && c.StoryTellerId == userId);
        return isSt ? encounter : RedactEncounterForPlayer(encounter, userId);
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

    /// <inheritdoc />
    public async Task ReorderInitiativeAsync(int encounterId, IReadOnlyList<int> entryIdsInOrder, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "reorder initiative");

        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        for (int i = 0; i < entryIdsInOrder.Count; i++)
        {
            int id = entryIdsInOrder[i];
            InitiativeEntry? entry = entries.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                entry.Order = i + 1;
            }
        }

        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task HoldActionAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        InitiativeEntry? currentActor = await _dbContext.InitiativeEntries
            .Where(e => e.EncounterId == encounterId && !e.HasActed && !e.IsHeld)
            .OrderBy(e => e.Order)
            .FirstOrDefaultAsync();

        if (currentActor != null)
        {
            currentActor.IsHeld = true;
            await _dbContext.SaveChangesAsync();
            await RecalculateOrderAsync(encounterId);
            await PublishInitiativeAsync(encounterId, storyTellerUserId);
        }
    }

    /// <inheritdoc />
    public async Task ReleaseHeldActionAsync(int encounterId, int entryId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        InitiativeEntry? released = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.EncounterId == encounterId && e.IsHeld);

        if (released == null)
        {
            return;
        }

        released.IsHeld = false;

        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(e => e.EncounterId == encounterId)
            .ToListAsync();

        static int Tier(InitiativeEntry e) => e.HasActed ? 2 : (e.IsHeld ? 1 : 0);

        List<InitiativeEntry> tier0 = entries.Where(e => Tier(e) == 0).OrderBy(e => e.Order).ToList();
        tier0.Remove(released);
        tier0.Insert(0, released);

        List<InitiativeEntry> sorted = [.. tier0, .. entries.Where(e => Tier(e) == 1).OrderByDescending(e => e.Total), .. entries.Where(e => Tier(e) == 2).OrderByDescending(e => e.Total)];

        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].Order = i + 1;
        }

        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private static CombatEncounter RedactEncounterForPlayer(CombatEncounter source, string viewerUserId)
    {
        CombatEncounter clone = new() { Id = source.Id, CampaignId = source.CampaignId, Name = source.Name, IsActive = source.IsActive, IsDraft = source.IsDraft, IsPaused = source.IsPaused, CurrentRound = source.CurrentRound, CreatedAt = source.CreatedAt, ResolvedAt = source.ResolvedAt, InitiativeEntries = [], NpcTemplates = [] };
        foreach (var entry in source.InitiativeEntries)
        {
            string? name = entry.CharacterId != null ? null : (entry.IsRevealed ? entry.NpcName : (string.IsNullOrWhiteSpace(entry.MaskedDisplayName) ? "Unknown" : entry.MaskedDisplayName.Trim()));
            InitiativeEntry copy = new() { Id = entry.Id, EncounterId = entry.EncounterId, CharacterId = entry.CharacterId, NpcName = name, InitiativeMod = entry.InitiativeMod, RollResult = entry.RollResult, Total = entry.Total, HasActed = entry.HasActed, IsHeld = entry.IsHeld, IsRevealed = entry.IsRevealed, MaskedDisplayName = entry.MaskedDisplayName, Order = entry.Order, NpcHealthBoxes = entry.NpcHealthBoxes, NpcHealthDamage = string.Empty, NpcMaxWillpower = entry.NpcMaxWillpower, NpcCurrentWillpower = entry.NpcCurrentWillpower, NpcMaxVitae = 0, NpcCurrentVitae = 0 };
            if (entry.Character != null)
            {
                copy.Character = entry.Character.ApplicationUserId == viewerUserId ? entry.Character : new Character { Id = entry.Character.Id, Name = entry.Character.Name, ApplicationUserId = string.Empty };
            }

            clone.InitiativeEntries.Add(copy);
        }

        return clone;
    }

    private async Task EnsureNoOtherOpenEncounterAsync(int campaignId, int exceptEncounterId)
    {
        bool conflict = await _dbContext.CombatEncounters.AnyAsync(e =>
            e.CampaignId == campaignId && e.Id != exceptEncounterId && !e.IsDraft && e.ResolvedAt == null && (e.IsActive || e.IsPaused));
        if (conflict)
        {
            throw new InvalidOperationException("Another encounter is already in progress or paused.");
        }
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

    private async Task<InitiativeEntry> AddCharacterToEncounterCoreAsync(int encounterId, int characterId, int initiativeMod, int rollResult)
    {
        InitiativeEntry entry = new() { EncounterId = encounterId, CharacterId = characterId, InitiativeMod = initiativeMod, RollResult = rollResult, Total = initiativeMod + rollResult, HasActed = false, IsHeld = false, IsRevealed = true };
        _dbContext.InitiativeEntries.Add(entry);
        return await Task.FromResult(entry);
    }
}
