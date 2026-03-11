using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for combat encounters and initiative tracking.
/// All mutating operations verify the caller is the campaign Storyteller.
/// Initiative order is maintained by a stable sort: Total descending,
/// then InitiativeMod descending, then player characters before NPCs.
/// </summary>
public class EncounterService(
    ApplicationDbContext dbContext,
    ILogger<EncounterService> logger,
    IAuthorizationHelper authHelper) : IEncounterService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<EncounterService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<CombatEncounter> CreateEncounterAsync(int campaignId, string name, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "manage encounters");

        CombatEncounter encounter = new()
        {
            CampaignId = campaignId,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _dbContext.CombatEncounters.Add(encounter);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Encounter {EncounterName} (Id={EncounterId}) created in campaign {CampaignId} by ST {UserId}",
            name,
            encounter.Id,
            campaignId,
            storyTellerUserId);

        return encounter;
    }

    /// <inheritdoc />
    public async Task<InitiativeEntry> AddCharacterToEncounterAsync(
        int encounterId,
        int characterId,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        InitiativeEntry entry = new()
        {
            EncounterId = encounterId,
            CharacterId = characterId,
            InitiativeMod = initiativeMod,
            RollResult = rollResult,
            Total = initiativeMod + rollResult,
            HasActed = false,
        };

        _dbContext.InitiativeEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        await RecalculateOrderAsync(encounterId);

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
        CombatEncounter encounter = await LoadActiveEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        InitiativeEntry entry = new()
        {
            EncounterId = encounterId,
            NpcName = npcName,
            InitiativeMod = initiativeMod,
            RollResult = rollResult,
            Total = initiativeMod + rollResult,
            HasActed = false,
        };

        _dbContext.InitiativeEntries.Add(entry);
        await _dbContext.SaveChangesAsync();

        await RecalculateOrderAsync(encounterId);

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
        CombatEncounter encounter = await LoadActiveEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .OrderBy(i => i.Order)
            .ToListAsync();

        if (entries.Count == 0)
        {
            return;
        }

        // Find the first entry that has not yet acted this round.
        InitiativeEntry? current = entries.FirstOrDefault(i => !i.HasActed);

        if (current == null)
        {
            // All participants have acted — reset for a new round.
            foreach (InitiativeEntry entry in entries)
            {
                entry.HasActed = false;
            }

            _logger.LogInformation(
                "Encounter {EncounterId}: new round started (all {Count} participants reset)",
                encounterId,
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
    }

    /// <inheritdoc />
    public async Task ResolveEncounterAsync(int encounterId, string storyTellerUserId)
    {
        CombatEncounter encounter = await LoadActiveEncounterAsync(encounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        encounter.IsActive = false;
        encounter.ResolvedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Encounter {EncounterId} resolved by ST {UserId}",
            encounterId,
            storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task<List<CombatEncounter>> GetEncountersAsync(int campaignId)
    {
        return await _dbContext.CombatEncounters
            .Where(e => e.CampaignId == campaignId)
            .OrderByDescending(e => e.IsActive)
            .ThenByDescending(e => e.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CombatEncounter?> GetEncounterAsync(int encounterId)
    {
        return await _dbContext.CombatEncounters
            .Include(e => e.InitiativeEntries.OrderBy(i => i.Order))
                .ThenInclude(i => i.Character)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == encounterId);
    }

    /// <inheritdoc />
    public async Task RemoveEntryAsync(int entryId, string storyTellerUserId)
    {
        InitiativeEntry entry = await _dbContext.InitiativeEntries
            .FirstOrDefaultAsync(i => i.Id == entryId)
            ?? throw new InvalidOperationException($"Initiative entry {entryId} not found.");

        CombatEncounter encounter = await LoadActiveEncounterAsync(entry.EncounterId);
        await _authHelper.RequireStorytellerAsync(encounter.CampaignId, storyTellerUserId, "manage encounters");

        _dbContext.InitiativeEntries.Remove(entry);
        await _dbContext.SaveChangesAsync();

        await RecalculateOrderAsync(entry.EncounterId);

        _logger.LogInformation(
            "Initiative entry {EntryId} removed from encounter {EncounterId} by ST {UserId}",
            entryId,
            entry.EncounterId,
            storyTellerUserId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Loads an active encounter or throws <see cref="InvalidOperationException"/>.</summary>
    private async Task<CombatEncounter> LoadActiveEncounterAsync(int encounterId)
    {
        CombatEncounter encounter = await _dbContext.CombatEncounters
            .FirstOrDefaultAsync(e => e.Id == encounterId)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (!encounter.IsActive)
        {
            throw new InvalidOperationException($"Encounter {encounterId} is already resolved.");
        }

        return encounter;
    }

    /// <summary>
    /// Re-sorts all entries for <paramref name="encounterId"/> and writes their
    /// <see cref="InitiativeEntry.Order"/> (1-indexed).
    /// Sort rule: Total desc → InitiativeMod desc → player characters before NPCs.
    /// </summary>
    private async Task RecalculateOrderAsync(int encounterId)
    {
        List<InitiativeEntry> entries = await _dbContext.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        IOrderedEnumerable<InitiativeEntry> sorted = entries
            .OrderByDescending(i => i.Total)
            .ThenByDescending(i => i.InitiativeMod)
            .ThenBy(i => i.CharacterId == null ? 1 : 0); // PCs (CharacterId set) act before NPCs on a tie

        int order = 1;
        foreach (InitiativeEntry entry in sorted)
        {
            entry.Order = order++;
        }

        await _dbContext.SaveChangesAsync();
    }
}
