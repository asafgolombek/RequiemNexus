using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages participant entries in a combat encounter: adding and removing characters and NPCs.
/// </summary>
public interface IEncounterParticipantService
{
    /// <summary>Adds online player characters to the encounter with server-side initiative rolls.</summary>
    Task BulkAddOnlinePlayersAsync(int encounterId, IReadOnlyList<int> characterIds, string storyTellerUserId);

    /// <summary>Adds a player character to an encounter and recalculates the initiative order.</summary>
    Task<InitiativeEntry> AddCharacterToEncounterAsync(
        int encounterId,
        int characterId,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId);

    /// <summary>Adds a named NPC to an encounter and recalculates the initiative order.</summary>
    Task<InitiativeEntry> AddNpcToEncounterAsync(
        int encounterId,
        string npcName,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId,
        int npcHealthBoxes = 7,
        int npcMaxWillpower = 4,
        int? chronicleNpcId = null,
        int npcMaxVitae = 0);

    /// <summary>Adds an active initiative row from a chronicle NPC (encounter must be active).</summary>
    Task<InitiativeEntry> AddNpcToEncounterFromChronicleNpcAsync(
        int encounterId,
        int chronicleNpcId,
        int initiativeMod,
        int rollResult,
        int healthBoxes,
        int maxWillpower,
        int maxVitae,
        string storyTellerUserId);

    /// <summary>Removes a participant from an encounter and recalculates the initiative order.</summary>
    Task RemoveEntryAsync(int entryId, string storyTellerUserId);
}
