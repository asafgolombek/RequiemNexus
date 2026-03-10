using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages combat encounters and initiative tracking for a campaign.
/// <para>
/// <see cref="CreateEncounterAsync"/>, <see cref="AdvanceTurnAsync"/>, and
/// <see cref="ResolveEncounterAsync"/> are restricted to the campaign Storyteller.
/// Adding participants is also ST-only at this layer.
/// </para>
/// </summary>
public interface IEncounterService
{
    /// <summary>
    /// Creates a new active encounter for the campaign.
    /// Only the campaign Storyteller may call this.
    /// </summary>
    Task<CombatEncounter> CreateEncounterAsync(int campaignId, string name, string storyTellerUserId);

    /// <summary>
    /// Adds a player character to an encounter and recalculates the initiative order.
    /// Only the campaign Storyteller may call this.
    /// </summary>
    Task<InitiativeEntry> AddCharacterToEncounterAsync(
        int encounterId,
        int characterId,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId);

    /// <summary>
    /// Adds a named NPC to an encounter and recalculates the initiative order.
    /// Only the campaign Storyteller may call this.
    /// </summary>
    Task<InitiativeEntry> AddNpcToEncounterAsync(
        int encounterId,
        string npcName,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId);

    /// <summary>
    /// Marks the next unacted participant as having acted.
    /// When all participants have acted the round resets (<see cref="InitiativeEntry.HasActed"/> → false for all).
    /// Only the campaign Storyteller may call this.
    /// </summary>
    Task AdvanceTurnAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Closes the encounter: sets <see cref="CombatEncounter.IsActive"/> to false and records
    /// <see cref="CombatEncounter.ResolvedAt"/>.
    /// Only the campaign Storyteller may call this.
    /// </summary>
    Task ResolveEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>Returns all encounters for a campaign — active encounters first, then by creation date descending.</summary>
    Task<List<CombatEncounter>> GetEncountersAsync(int campaignId);

    /// <summary>
    /// Returns a single encounter with its <see cref="CombatEncounter.InitiativeEntries"/>
    /// pre-sorted by <see cref="InitiativeEntry.Order"/>.
    /// Returns <see langword="null"/> if the encounter does not exist.
    /// </summary>
    Task<CombatEncounter?> GetEncounterAsync(int encounterId);

    /// <summary>
    /// Removes a participant from an encounter and recalculates the initiative order.
    /// Only the campaign Storyteller may call this.
    /// </summary>
    Task RemoveEntryAsync(int entryId, string storyTellerUserId);
}
