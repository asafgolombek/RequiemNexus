using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Read-only queries for combat encounters.
/// Mutations are handled by <see cref="EncounterService"/> and <see cref="EncounterParticipantService"/>.
/// </summary>
public class EncounterQueryService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper) : IEncounterQueryService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;

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
}
