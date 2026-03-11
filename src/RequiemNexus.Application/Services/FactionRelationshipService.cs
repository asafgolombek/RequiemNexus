using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for directional faction relationship management. All mutations are Storyteller-only.
/// </summary>
public class FactionRelationshipService(
    ApplicationDbContext dbContext,
    ILogger<FactionRelationshipService> logger,
    IAuthorizationHelper authHelper) : IFactionRelationshipService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<FactionRelationshipService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<List<FactionRelationship>> GetRelationshipsAsync(int campaignId)
    {
        return await _dbContext.FactionRelationships
            .Include(r => r.FactionA)
            .Include(r => r.FactionB)
            .Where(r => r.CampaignId == campaignId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<FactionRelationship> SetRelationshipAsync(
        int campaignId,
        int factionAId,
        int factionBId,
        FactionStance stance,
        string notes,
        string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "modify the Danse Macabre");

        FactionRelationship? existing = await _dbContext.FactionRelationships
            .FirstOrDefaultAsync(r => r.CampaignId == campaignId
                                   && r.FactionAId == factionAId
                                   && r.FactionBId == factionBId);

        if (existing is not null)
        {
            existing.StanceFromA = stance;
            existing.Notes = notes;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        FactionRelationship relationship = new()
        {
            CampaignId = campaignId,
            FactionAId = factionAId,
            FactionBId = factionBId,
            StanceFromA = stance,
            Notes = notes,
        };

        _dbContext.FactionRelationships.Add(relationship);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction relationship {FactionAId} → {FactionBId} set to {Stance} in campaign {CampaignId} by ST {UserId}",
            factionAId,
            factionBId,
            stance,
            campaignId,
            stUserId);

        return relationship;
    }

    /// <inheritdoc />
    public async Task DeleteRelationshipAsync(int relationshipId, string stUserId)
    {
        FactionRelationship relationship = await _dbContext.FactionRelationships.FindAsync(relationshipId)
            ?? throw new InvalidOperationException($"Relationship {relationshipId} not found.");

        await _authHelper.RequireStorytellerAsync(relationship.CampaignId, stUserId, "modify the Danse Macabre");

        _dbContext.FactionRelationships.Remove(relationship);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Faction relationship {RelationshipId} deleted by ST {UserId}",
            relationshipId,
            stUserId);
    }
}
