using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages city factions within a campaign. All mutating operations are Storyteller-only.
/// </summary>
public interface ICityFactionService
{
    /// <summary>Returns all factions in the campaign, including their leader NPC, members, and controlled territories.</summary>
    /// <param name="campaignId">The campaign to load factions for.</param>
    Task<List<CityFaction>> GetFactionsAsync(int campaignId);

    /// <summary>Returns a single faction with its members and territories, or <c>null</c> if not found.</summary>
    /// <param name="factionId">The faction to load.</param>
    Task<CityFaction?> GetFactionAsync(int factionId);

    /// <summary>Creates a new faction. ST-only.</summary>
    /// <param name="campaignId">The campaign the faction belongs to.</param>
    /// <param name="name">Display name of the faction.</param>
    /// <param name="type">The faction type (covenant, mortal organisation, etc.).</param>
    /// <param name="influenceRating">Influence level from 1–5.</param>
    /// <param name="publicDescription">Text visible to players.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task<CityFaction> CreateFactionAsync(int campaignId, string name, FactionType type, int influenceRating, string publicDescription, string stUserId);

    /// <summary>Updates editable fields on the faction. ST-only.</summary>
    /// <param name="factionId">The faction to update.</param>
    /// <param name="name">New display name.</param>
    /// <param name="type">New faction type.</param>
    /// <param name="influenceRating">New influence rating.</param>
    /// <param name="publicDescription">New player-visible description.</param>
    /// <param name="storytellerNotes">ST-only notes.</param>
    /// <param name="agenda">The faction's secret agenda.</param>
    /// <param name="leaderNpcId">Optional NPC designated as leader.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task UpdateFactionAsync(int factionId, string name, FactionType type, int influenceRating, string publicDescription, string storytellerNotes, string agenda, int? leaderNpcId, string stUserId);

    /// <summary>Deletes the faction. ST-only.</summary>
    /// <param name="factionId">The faction to delete.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task DeleteFactionAsync(int factionId, string stUserId);
}
