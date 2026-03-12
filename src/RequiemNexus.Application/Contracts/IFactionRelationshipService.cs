using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages directional stance relationships between city factions. All mutating operations are Storyteller-only.
/// </summary>
public interface IFactionRelationshipService
{
    /// <summary>Returns all faction relationships in the campaign, including both faction names.</summary>
    /// <param name="campaignId">The campaign to load relationships for.</param>
    Task<List<FactionRelationship>> GetRelationshipsAsync(int campaignId);

    /// <summary>
    /// Creates or updates the directional stance from Faction A toward Faction B. ST-only.
    /// If a relationship already exists for the (FactionA, FactionB) pair it is updated; otherwise a new one is created.
    /// </summary>
    /// <param name="campaignId">The campaign the relationship belongs to.</param>
    /// <param name="factionAId">The source faction.</param>
    /// <param name="factionBId">The target faction.</param>
    /// <param name="stance">The stance Faction A holds toward Faction B.</param>
    /// <param name="notes">ST narrative notes on the relationship.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task<FactionRelationship> SetRelationshipAsync(int campaignId, int factionAId, int factionBId, FactionStance stance, string notes, string stUserId);

    /// <summary>Deletes a faction relationship. ST-only.</summary>
    /// <param name="relationshipId">The relationship to delete.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task DeleteRelationshipAsync(int relationshipId, string stUserId);
}
