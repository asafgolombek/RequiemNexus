using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for city faction CRUD. All mutations are Storyteller-only.
/// </summary>
public class CityFactionService(
    ApplicationDbContext dbContext,
    ILogger<CityFactionService> logger,
    IAuthorizationHelper authHelper) : ICityFactionService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<CityFactionService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<List<CityFaction>> GetFactionsAsync(int campaignId)
    {
        return await _dbContext.CityFactions
            .Include(f => f.LeaderNpc)
            .Include(f => f.Members)
            .Include(f => f.ControlledTerritories)
            .Where(f => f.CampaignId == campaignId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CityFaction?> GetFactionAsync(int factionId)
    {
        return await _dbContext.CityFactions
            .Include(f => f.LeaderNpc)
            .Include(f => f.Members)
            .Include(f => f.ControlledTerritories)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == factionId);
    }

    /// <inheritdoc />
    public async Task<CityFaction> CreateFactionAsync(
        int campaignId,
        string name,
        FactionType type,
        int influenceRating,
        string publicDescription,
        string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "modify the Danse Macabre");

        CityFaction faction = new()
        {
            CampaignId = campaignId,
            Name = name,
            FactionType = type,
            InfluenceRating = influenceRating,
            PublicDescription = publicDescription,
        };

        _dbContext.CityFactions.Add(faction);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction '{Name}' (Id={FactionId}) created in campaign {CampaignId} by ST {UserId}",
            faction.Name,
            faction.Id,
            campaignId,
            stUserId);

        return faction;
    }

    /// <inheritdoc />
    public async Task UpdateFactionAsync(
        int factionId,
        string name,
        FactionType type,
        int influenceRating,
        string publicDescription,
        string storytellerNotes,
        string agenda,
        int? leaderNpcId,
        string stUserId)
    {
        CityFaction faction = await _dbContext.CityFactions.FindAsync(factionId)
            ?? throw new InvalidOperationException($"Faction {factionId} not found.");

        await _authHelper.RequireStorytellerAsync(faction.CampaignId, stUserId, "modify the Danse Macabre");

        faction.Name = name;
        faction.FactionType = type;
        faction.InfluenceRating = influenceRating;
        faction.PublicDescription = publicDescription;
        faction.StorytellerNotes = storytellerNotes;
        faction.Agenda = agenda;
        faction.LeaderNpcId = leaderNpcId;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction {FactionId} updated by ST {UserId}",
            factionId,
            stUserId);
    }

    /// <inheritdoc />
    public async Task DeleteFactionAsync(int factionId, string stUserId)
    {
        CityFaction faction = await _dbContext.CityFactions.FindAsync(factionId)
            ?? throw new InvalidOperationException($"Faction {factionId} not found.");

        await _authHelper.RequireStorytellerAsync(faction.CampaignId, stUserId, "modify the Danse Macabre");

        _dbContext.CityFactions.Remove(faction);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction {FactionId} deleted by ST {UserId}",
            factionId,
            stUserId);
    }
}
