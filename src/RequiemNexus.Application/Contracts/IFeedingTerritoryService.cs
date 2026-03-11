using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages feeding territories within a campaign. All mutating operations are Storyteller-only.
/// </summary>
public interface IFeedingTerritoryService
{
    /// <summary>Returns all feeding territories in the campaign, including the controlling faction.</summary>
    /// <param name="campaignId">The campaign to load territories for.</param>
    Task<List<FeedingTerritory>> GetTerritoriesAsync(int campaignId);

    /// <summary>Creates a new feeding territory. ST-only.</summary>
    /// <param name="campaignId">The campaign the territory belongs to.</param>
    /// <param name="name">Display name of the territory.</param>
    /// <param name="description">Narrative description of the hunting ground.</param>
    /// <param name="rating">Feeding quality from 1–5.</param>
    /// <param name="controlledByFactionId">Optional faction that controls this territory.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task<FeedingTerritory> CreateTerritoryAsync(int campaignId, string name, string description, int rating, int? controlledByFactionId, string stUserId);

    /// <summary>Updates editable fields on the territory. ST-only.</summary>
    /// <param name="territoryId">The territory to update.</param>
    /// <param name="name">New display name.</param>
    /// <param name="description">New description.</param>
    /// <param name="rating">New quality rating.</param>
    /// <param name="controlledByFactionId">New controlling faction.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task UpdateTerritoryAsync(int territoryId, string name, string description, int rating, int? controlledByFactionId, string stUserId);

    /// <summary>Deletes the territory. ST-only.</summary>
    /// <param name="territoryId">The territory to delete.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task DeleteTerritoryAsync(int territoryId, string stUserId);
}
