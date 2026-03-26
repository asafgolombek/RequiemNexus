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
}
