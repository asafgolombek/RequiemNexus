using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages the political and social landscape (Danse Macabre) of a campaign:
/// factions, chronicle NPCs, feeding territories, and faction relationships.
/// All mutating operations are Storyteller-only.
/// </summary>
public interface IDanseMacabreService
{
    // ── Factions ─────────────────────────────────────────────────────────────

    /// <summary>Returns all factions in the campaign.</summary>
    Task<List<CityFaction>> GetFactionsAsync(int campaignId);

    /// <summary>Returns a single faction with its members and territories, or <c>null</c> if not found.</summary>
    Task<CityFaction?> GetFactionAsync(int factionId);

    /// <summary>Creates a new faction. ST-only.</summary>
    Task<CityFaction> CreateFactionAsync(int campaignId, string name, FactionType type, int influenceRating, string publicDescription, string stUserId);

    /// <summary>Updates editable fields on the faction. ST-only.</summary>
    Task UpdateFactionAsync(int factionId, string name, FactionType type, int influenceRating, string publicDescription, string storytellerNotes, string agenda, int? leaderNpcId, string stUserId);

    /// <summary>Deletes the faction. ST-only.</summary>
    Task DeleteFactionAsync(int factionId, string stUserId);

    // ── NPCs ──────────────────────────────────────────────────────────────────

    /// <summary>Returns all NPCs in the campaign, optionally including deceased ones.</summary>
    Task<List<ChronicleNpc>> GetNpcsAsync(int campaignId, bool includeDeceased = false);

    /// <summary>Returns a single NPC, or <c>null</c> if not found.</summary>
    Task<ChronicleNpc?> GetNpcAsync(int npcId);

    /// <summary>Creates a new chronicle NPC. ST-only.</summary>
    Task<ChronicleNpc> CreateNpcAsync(int campaignId, string name, string? title, int? primaryFactionId, string? roleInFaction, string publicDescription, string stUserId);

    /// <summary>Updates editable fields on the NPC. ST-only.</summary>
    Task UpdateNpcAsync(int npcId, string name, string? title, int? primaryFactionId, string? roleInFaction, string publicDescription, string storytellerNotes, int? linkedStatBlockId, bool isVampire, string attributesJson, string skillsJson, string stUserId);

    /// <summary>Sets the NPC's alive/deceased state. ST-only.</summary>
    Task SetNpcAliveAsync(int npcId, bool isAlive, string stUserId);

    /// <summary>Deletes the NPC. ST-only.</summary>
    Task DeleteNpcAsync(int npcId, string stUserId);

    // ── Territories ───────────────────────────────────────────────────────────

    /// <summary>Returns all feeding territories in the campaign.</summary>
    Task<List<FeedingTerritory>> GetTerritoriesAsync(int campaignId);

    /// <summary>Creates a new feeding territory. ST-only.</summary>
    Task<FeedingTerritory> CreateTerritoryAsync(int campaignId, string name, string description, int rating, int? controlledByFactionId, string stUserId);

    /// <summary>Updates editable fields on the territory. ST-only.</summary>
    Task UpdateTerritoryAsync(int territoryId, string name, string description, int rating, int? controlledByFactionId, string stUserId);

    /// <summary>Deletes the territory. ST-only.</summary>
    Task DeleteTerritoryAsync(int territoryId, string stUserId);

    // ── Relationships ─────────────────────────────────────────────────────────

    /// <summary>Returns all faction relationships in the campaign.</summary>
    Task<List<FactionRelationship>> GetRelationshipsAsync(int campaignId);

    /// <summary>
    /// Creates or updates the relationship from Faction A toward Faction B. ST-only.
    /// If a relationship already exists for the (FactionA, FactionB) pair it is updated; otherwise created.
    /// </summary>
    Task<FactionRelationship> SetRelationshipAsync(int campaignId, int factionAId, int factionBId, FactionStance stance, string notes, string stUserId);

    /// <summary>Deletes a faction relationship. ST-only.</summary>
    Task DeleteRelationshipAsync(int relationshipId, string stUserId);
}
