using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for feeding territory CRUD. All mutations are Storyteller-only.
/// </summary>
public class FeedingTerritoryService(
    ApplicationDbContext dbContext,
    ILogger<FeedingTerritoryService> logger,
    IAuthorizationHelper authHelper) : IFeedingTerritoryService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<FeedingTerritoryService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<List<FeedingTerritory>> GetTerritoriesAsync(int campaignId)
    {
        return await _dbContext.FeedingTerritories
            .Include(t => t.ControlledByFaction)
            .Where(t => t.CampaignId == campaignId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FeedingTerritory> CreateTerritoryAsync(
        int campaignId,
        string name,
        string description,
        int rating,
        int? controlledByFactionId,
        string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "modify the Danse Macabre");

        FeedingTerritory territory = new()
        {
            CampaignId = campaignId,
            Name = name,
            Description = description,
            Rating = rating,
            ControlledByFactionId = controlledByFactionId,
        };

        _dbContext.FeedingTerritories.Add(territory);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Territory '{Name}' (Id={TerritoryId}) created in campaign {CampaignId} by ST {UserId}",
            territory.Name,
            territory.Id,
            campaignId,
            stUserId);

        return territory;
    }

    /// <inheritdoc />
    public async Task UpdateTerritoryAsync(
        int territoryId,
        string name,
        string description,
        int rating,
        int? controlledByFactionId,
        string stUserId)
    {
        FeedingTerritory territory = await _dbContext.FeedingTerritories.FindAsync(territoryId)
            ?? throw new InvalidOperationException($"Territory {territoryId} not found.");

        await _authHelper.RequireStorytellerAsync(territory.CampaignId, stUserId, "modify the Danse Macabre");

        territory.Name = name;
        territory.Description = description;
        territory.Rating = rating;
        territory.ControlledByFactionId = controlledByFactionId;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Territory {TerritoryId} updated by ST {UserId}",
            territoryId,
            stUserId);
    }

    /// <inheritdoc />
    public async Task DeleteTerritoryAsync(int territoryId, string stUserId)
    {
        FeedingTerritory territory = await _dbContext.FeedingTerritories.FindAsync(territoryId)
            ?? throw new InvalidOperationException($"Territory {territoryId} not found.");

        await _authHelper.RequireStorytellerAsync(territory.CampaignId, stUserId, "modify the Danse Macabre");

        _dbContext.FeedingTerritories.Remove(territory);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Territory {TerritoryId} deleted by ST {UserId}",
            territoryId,
            stUserId);
    }
}
