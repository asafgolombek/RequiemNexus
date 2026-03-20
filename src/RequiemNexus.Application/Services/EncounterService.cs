using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for combat encounters and initiative tracking.
/// Mutating operations verify the caller is the campaign Storyteller (except read APIs for members).
/// Publishes initiative to Redis/SignalR after successful writes when the encounter is active.
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
    public async Task<CombatEncounter> CreateDraftEncounterAsync(int campaignId, string name, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "manage encounters");

        CombatEncounter encounter = new()
        {
            CampaignId = campaignId,
            Name = name,
            IsDraft = true,
            IsActive = false,
            IsPaused = false,
            CurrentRound = 1,
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.CombatEncounters.Add(encounter);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Draft encounter {EncounterName} (Id={EncounterId}) created in campaign {CampaignId} by ST {UserId}",
            name,
            encounter.Id,
            campaignId,
            storyTellerUserId);

        return encounter;
    }

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

        if (encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        await EnsureNoOtherOpenEncounterAsync(encounter.CampaignId, exceptEncounterId: encounterId);

        foreach (EncounterNpcTemplate template in encounter.NpcTemplates)
        {
            int roll = Random.Shared.Next(1, 11);
            InitiativeEntry entry = new()
            {
                EncounterId = encounterId,
                NpcName = template.Name,
                InitiativeMod = template.InitiativeMod,
                RollResult = roll,
                Total = template.InitiativeMod + roll,
                HasActed = false,
                IsHeld = false,
                IsRevealed = template.IsRevealed,
                MaskedDisplayName = template.DefaultMaskedName,
                NpcHealthBoxes = template.HealthBoxes,
                NpcHealthDamage = string.Empty,
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
    public async Task<EncounterNpcTemplate> AddNpcTemplateAsync(
        int encounterId,
        string name,
        int initiativeMod,
        int healthBoxes,
        string? notes,
        bool isRevealed,
        string? defaultMaskedName,
        string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadDraftEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        EncounterNpcTemplate row = new()
        {
            EncounterId = encounterId,
            Name = name,
            InitiativeMod = initiativeMod,
            HealthBoxes = healthBoxes,
            Notes = notes,
            IsRevealed = isRevealed,
            DefaultMaskedName = defaultMaskedName,
        };

        _dbContext.Set<EncounterNpcTemplate>().Add(row);
        await _dbContext.SaveChangesAsync();

        return row;
    }

    /// <inheritdoc />
    public async Task RemoveNpcTemplateAsync(int templateId, string storyTellerUserId)
    {
        EncounterNpcTemplate row = await _dbContext.Set<EncounterNpcTemplate>()
            .FirstOrDefaultAsync(t => t.Id == templateId)
            ?? throw new InvalidOperationException($"NPC template {templateId} not found.");

        CombatEncounter encounter = await LoadDraftEncounterAsync(row.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        _dbContext.Set<EncounterNpcTemplate>().Remove(row);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task BulkAddOnlinePlayersAsync(int encounterId, IReadOnlyList<int> characterIds, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        SessionStateDto? session = await _sessionService.GetSessionStateAsync(encounter.CampaignId);
        HashSet<int> onlineCharIds = session?.Presence
            .Where(p => p.IsOnline && p.CharacterId.HasValue)
            .Select(p => p.CharacterId!.Value)
            .ToHashSet() ?? [];

        List<InitiativeEntry> existing = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId && i.CharacterId != null)
            .ToListAsync();
        HashSet<int> alreadyIn = existing.Select(e => e.CharacterId!.Value).ToHashSet();

        foreach (int characterId in characterIds.Distinct())
        {
            if (alreadyIn.Contains(characterId))
            {
                continue;
            }

            if (!onlineCharIds.Contains(characterId))
            {
                _logger.LogWarning(
                    "Skipping character {CharacterId} for bulk initiative — not online in session for campaign {CampaignId}",
                    characterId,
                    encounter.CampaignId);
                continue;
            }

            Character? character = await _dbContext.Characters
                .Include(c => c.Attributes)
                .FirstOrDefaultAsync(c => c.Id == characterId && c.CampaignId == encounter.CampaignId);

            if (character == null)
            {
                _logger.LogWarning(
                    "Skipping character {CharacterId} — not in campaign {CampaignId}",
                    characterId,
                    encounter.CampaignId);
                continue;
            }

            int wits = character.GetAttributeRating(AttributeId.Wits);
            int composure = character.GetAttributeRating(AttributeId.Composure);
            int mod = wits + composure;
            int roll = Random.Shared.Next(1, 11);

            await AddCharacterToEncounterCoreAsync(encounterId, characterId, mod, roll);
        }

        await _dbContext.SaveChangesAsync();
        await RecalculateOrderAsync(encounterId);
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
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

        _logger.LogInformation(
            "Character {CharacterId} added to encounter {EncounterId} with initiative {Total}",
            characterId,
            encounterId,
            entry.Total);

        return entry;
    }

    /// <inheritdoc />
    public async Task<InitiativeEntry> AddNpcToEncounterAsync(
        int encounterId,
        string npcName,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadMutableEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        InitiativeEntry entry = new()
        {
            EncounterId = encounterId,
            NpcName = npcName,
            InitiativeMod = initiativeMod,
            RollResult = rollResult,
            Total = initiativeMod + rollResult,
            HasActed = false,
            IsHeld = false,
            IsRevealed = true,
            NpcHealthBoxes = 7,
            NpcHealthDamage = string.Empty,
        };

        _dbContext.InitiativeEntries.Add(entry);
        await _dbContext.SaveChangesAsync();
        await RecalculateOrderAsync(encounterId);
        await PublishInitiativeIfActiveAsync(encounter, storyTellerUserId);

        _logger.LogInformation(
            "NPC {NpcName} added to encounter {EncounterId} with initiative {Total}",
            npcName,
            encounterId,
            entry.Total);

        return entry;
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

        if (entries.Count == 0)
        {
            return;
        }

        InitiativeEntry? current = entries.FirstOrDefault(i => !i.HasActed);

        if (current == null)
        {
            foreach (InitiativeEntry entry in entries)
            {
                entry.HasActed = false;
                entry.IsHeld = false;
            }

            encounter.CurrentRound++;
            _logger.LogInformation(
                "Encounter {EncounterId}: new round {Round} (all {Count} participants reset)",
                encounterId,
                encounter.CurrentRound,
                entries.Count);
        }
        else
        {
            current.HasActed = true;

            _logger.LogInformation(
                "Encounter {EncounterId}: participant {EntryId} marked as acted",
                encounterId,
                current.Id);
        }

        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task ResolveEncounterAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (encounter.IsDraft)
        {
            throw new InvalidOperationException("Only a started encounter can be resolved.");
        }

        if (encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        if (!encounter.IsActive && !encounter.IsPaused)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is not running or paused.");
        }

        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        encounter.IsActive = false;
        encounter.IsPaused = false;
        encounter.ResolvedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        await _sessionService.UpdateInitiativeAsync(storyTellerUserId, encounter.CampaignId, Array.Empty<InitiativeEntryDto>());

        _logger.LogInformation(
            "Encounter {EncounterId} resolved by ST {UserId}",
            encounterId,
            storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task PauseEncounterAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        encounter.IsActive = false;
        encounter.IsPaused = true;
        await _dbContext.SaveChangesAsync();

        await _sessionService.UpdateInitiativeAsync(storyTellerUserId, encounter.CampaignId, Array.Empty<InitiativeEntryDto>());

        _logger.LogInformation(
            "Encounter {EncounterId} paused by ST {UserId}",
            encounterId,
            storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task ResumeEncounterAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (!encounter.IsPaused || encounter.IsDraft || encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is not paused or cannot be resumed.");
        }

        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        await EnsureNoOtherOpenEncounterAsync(encounter.CampaignId, exceptEncounterId: encounterId);

        encounter.IsPaused = false;
        encounter.IsActive = true;
        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(encounterId, storyTellerUserId);

        _logger.LogInformation(
            "Encounter {EncounterId} resumed by ST {UserId}",
            encounterId,
            storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task<List<CombatEncounter>> GetEncountersAsync(int campaignId, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "view encounters");

        return await _dbContext.CombatEncounters
            .AsNoTracking()
            .Include(e => e.NpcTemplates)
            .Where(e => e.CampaignId == campaignId)
            .OrderByDescending(e => e.IsActive && !e.IsDraft)
            .ThenByDescending(e => e.IsPaused && !e.IsDraft && e.ResolvedAt == null)
            .ThenByDescending(e => e.IsDraft)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CombatEncounter?> GetEncounterAsync(int encounterId, string userId)
    {
        CombatEncounter? encounter = await _dbContext.CombatEncounters
            .AsNoTracking()
            .Include(e => e.InitiativeEntries.OrderBy(i => i.Order))
                .ThenInclude(i => i.Character)!
                    .ThenInclude(c => c!.Attributes)
            .Include(e => e.NpcTemplates)
            .FirstOrDefaultAsync(e => e.Id == encounterId);

        if (encounter == null)
        {
            return null;
        }

        await _authHelper.RequireCampaignMemberAsync(encounter.CampaignId, userId, "view this encounter");

        bool isSt = await IsCampaignStorytellerAsync(encounter.CampaignId, userId);
        if (isSt)
        {
            return encounter;
        }

        return RedactEncounterForPlayer(encounter, userId);
    }

    /// <inheritdoc />
    public async Task<CombatEncounter?> GetActiveEncounterForCampaignAsync(int campaignId, string userId)
    {
        await _authHelper.RequireCampaignMemberAsync(campaignId, userId, "view encounter state");

        CombatEncounter? encounter = await _dbContext.CombatEncounters
            .AsNoTracking()
            .Include(e => e.InitiativeEntries.OrderBy(i => i.Order))
                .ThenInclude(i => i.Character)!
                    .ThenInclude(c => c!.Attributes)
            .Where(e => e.CampaignId == campaignId && e.IsActive && !e.IsDraft)
            .FirstOrDefaultAsync();

        if (encounter == null)
        {
            return null;
        }

        bool isSt = await IsCampaignStorytellerAsync(campaignId, userId);
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

        _logger.LogInformation(
            "Initiative entry {EntryId} removed from encounter {EncounterId} by ST {UserId}",
            entryId,
            entry.EncounterId,
            storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task ReorderInitiativeAsync(int encounterId, IReadOnlyList<int> entryIdsInOrder, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        if (entryIdsInOrder.Count != entries.Count || entryIdsInOrder.Distinct().Count() != entries.Count)
        {
            throw new InvalidOperationException("Reorder list must include each initiative entry exactly once.");
        }

        HashSet<int> valid = entries.Select(e => e.Id).ToHashSet();
        if (entryIdsInOrder.Any(id => !valid.Contains(id)))
        {
            throw new InvalidOperationException("Reorder list contains unknown entry ids.");
        }

        for (int i = 0; i < entryIdsInOrder.Count; i++)
        {
            InitiativeEntry? row = entries.FirstOrDefault(e => e.Id == entryIdsInOrder[i]);
            if (row != null)
            {
                row.Order = i + 1;
            }
        }

        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task ApplyNpcDamageAsync(int entryId, char damageType, string storyTellerUserId)
    {
        if (damageType is not ('/' or 'X' or '*'))
        {
            throw new ArgumentException("Damage type must be '/', 'X', or '*'.", nameof(damageType));
        }

        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        if (entry.CharacterId != null)
        {
            throw new InvalidOperationException("NPC damage applies to NPC initiative rows only.");
        }

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        if (entry.NpcHealthDamage.Length >= entry.NpcHealthBoxes)
        {
            return;
        }

        entry.NpcHealthDamage += damageType;
        await _dbContext.SaveChangesAsync();
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
            throw new InvalidOperationException("NPC heal applies to NPC initiative rows only.");
        }

        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        if (entry.NpcHealthDamage.Length == 0)
        {
            return;
        }

        entry.NpcHealthDamage = entry.NpcHealthDamage[..^1];
        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(entry.EncounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task HoldActionAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .OrderBy(i => i.Order)
            .ToListAsync();

        InitiativeEntry? current = entries.FirstOrDefault(i => !i.HasActed);
        if (current == null)
        {
            return;
        }

        current.IsHeld = true;
        await _dbContext.SaveChangesAsync();
        await RecalculateOrderAsync(encounterId);
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task ReleaseHeldActionAsync(int encounterId, int entryId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveCombatEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        InitiativeEntry? released = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId && i.EncounterId == encounterId);

        if (released == null || !released.IsHeld)
        {
            throw new InvalidOperationException("Entry is not held in this encounter.");
        }

        released.IsHeld = false;

        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        List<InitiativeEntry> tier0 = entries
            .Where(e => Tier(e) == 0)
            .OrderBy(e => e.Order)
            .ToList();

        tier0.Remove(released);
        tier0.Insert(0, released);

        List<InitiativeEntry> tier1 = entries
            .Where(e => Tier(e) == 1)
            .OrderByDescending(e => e.Total)
            .ThenByDescending(e => e.InitiativeMod)
            .ThenBy(e => e.CharacterId == null ? 1 : 0)
            .ToList();

        List<InitiativeEntry> tier2 = entries
            .Where(e => Tier(e) == 2)
            .OrderByDescending(e => e.Total)
            .ThenByDescending(e => e.InitiativeMod)
            .ThenBy(e => e.CharacterId == null ? 1 : 0)
            .ToList();

        List<InitiativeEntry> combined = [];
        combined.AddRange(tier0);
        combined.AddRange(tier1);
        combined.AddRange(tier2);

        for (int i = 0; i < combined.Count; i++)
        {
            combined[i].Order = i + 1;
        }

        await _dbContext.SaveChangesAsync();
        await PublishInitiativeAsync(encounterId, storyTellerUserId);
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

    private static int Tier(InitiativeEntry e)
    {
        if (e.HasActed)
        {
            return 2;
        }

        return e.IsHeld ? 1 : 0;
    }

    private static CombatEncounter RedactEncounterForPlayer(CombatEncounter source, string viewerUserId)
    {
        CombatEncounter clone = new()
        {
            Id = source.Id,
            CampaignId = source.CampaignId,
            Name = source.Name,
            IsActive = source.IsActive,
            IsDraft = source.IsDraft,
            IsPaused = source.IsPaused,
            CurrentRound = source.CurrentRound,
            CreatedAt = source.CreatedAt,
            ResolvedAt = source.ResolvedAt,
            InitiativeEntries = [],
            NpcTemplates = [],
        };

        foreach (InitiativeEntry entry in source.InitiativeEntries)
        {
            string? npcDisplayName = entry.CharacterId != null
                ? null
                : entry.IsRevealed
                    ? entry.NpcName
                    : (string.IsNullOrWhiteSpace(entry.MaskedDisplayName) ? "Unknown" : entry.MaskedDisplayName.Trim());

            InitiativeEntry copy = new()
            {
                Id = entry.Id,
                EncounterId = entry.EncounterId,
                CharacterId = entry.CharacterId,
                NpcName = npcDisplayName,
                InitiativeMod = entry.InitiativeMod,
                RollResult = entry.RollResult,
                Total = entry.Total,
                HasActed = entry.HasActed,
                IsHeld = entry.IsHeld,
                IsRevealed = entry.IsRevealed,
                MaskedDisplayName = entry.MaskedDisplayName,
                Order = entry.Order,
                NpcHealthBoxes = entry.NpcHealthBoxes,
                NpcHealthDamage = string.Empty,
            };

            if (entry.Character != null)
            {
                copy.Character = entry.Character.ApplicationUserId == viewerUserId
                    ? entry.Character
                    : CreateRedactedCharacterStub(entry.Character);
            }

            clone.InitiativeEntries.Add(copy);
        }

        return clone;
    }

    private static Character CreateRedactedCharacterStub(Character source) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            ApplicationUserId = string.Empty,
            Size = 0,
            HealthDamage = string.Empty,
            Attributes = [],
            Skills = [],
            CharacterEquipments = [],
        };

    private Task<InitiativeEntry> AddCharacterToEncounterCoreAsync(
        int encounterId,
        int characterId,
        int initiativeMod,
        int rollResult)
    {
        InitiativeEntry entry = new()
        {
            EncounterId = encounterId,
            CharacterId = characterId,
            InitiativeMod = initiativeMod,
            RollResult = rollResult,
            Total = initiativeMod + rollResult,
            HasActed = false,
            IsHeld = false,
            IsRevealed = true,
        };

        _dbContext.InitiativeEntries.Add(entry);
        return Task.FromResult(entry);
    }

    /// <summary>
    /// At most one open fight per campaign: launched and not resolved (running or paused).
    /// </summary>
    private async Task EnsureNoOtherOpenEncounterAsync(int campaignId, int exceptEncounterId)
    {
        bool conflict = await _dbContext.CombatEncounters.AnyAsync(e =>
            e.CampaignId == campaignId
            && e.Id != exceptEncounterId
            && !e.IsDraft
            && e.ResolvedAt == null
            && (e.IsActive || e.IsPaused));

        if (conflict)
        {
            throw new InvalidOperationException(
                "Another encounter is already in progress or paused. Resolve or resume it before starting a new one.");
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

    private async Task<bool> IsCampaignStorytellerAsync(int campaignId, string userId) =>
        await _dbContext.Campaigns.AnyAsync(c => c.Id == campaignId && c.StoryTellerId == userId);

    private async Task<CombatEncounter> LoadDraftEncounterAsync(int encounterId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (!encounter.IsDraft)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is not a draft.");
        }

        if (encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        return encounter;
    }

    private async Task<CombatEncounter> LoadMutableEncounterAsync(int encounterId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (encounter.ResolvedAt != null)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        return encounter;
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

    /// <summary>
    /// Re-sorts entries using tier (unheld unacted → held unacted → acted) then initiative tie-breakers.
    /// </summary>
    private async Task RecalculateOrderAsync(int encounterId)
    {
        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        List<InitiativeEntry> sorted = entries
            .OrderBy(Tier)
            .ThenByDescending(i => i.Total)
            .ThenByDescending(i => i.InitiativeMod)
            .ThenBy(i => i.CharacterId == null ? 1 : 0)
            .ToList();

        int order = 1;
        foreach (InitiativeEntry entry in sorted)
        {
            entry.Order = order++;
        }

        await _dbContext.SaveChangesAsync();
    }
}
