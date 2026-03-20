using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages combat encounters and initiative tracking for a campaign.
/// </summary>
public interface IEncounterService
{
    /// <summary>
    /// Creates a draft encounter (prep). Launch activates it when the table is ready.
    /// </summary>
    Task<CombatEncounter> CreateDraftEncounterAsync(int campaignId, string name, string storyTellerUserId);

    /// <summary>
    /// Activates a draft encounter, rolls initiative for all NPC templates, and enforces a single active fight per campaign.
    /// </summary>
    Task LaunchEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Adds an NPC template row to a draft encounter.
    /// </summary>
    Task<EncounterNpcTemplate> AddNpcTemplateAsync(
        int encounterId,
        string name,
        int initiativeMod,
        int healthBoxes,
        string? notes,
        bool isRevealed,
        string? defaultMaskedName,
        string storyTellerUserId);

    /// <summary>
    /// Removes a template row from a draft encounter.
    /// </summary>
    Task RemoveNpcTemplateAsync(int templateId, string storyTellerUserId);

    /// <summary>
    /// Adds online player characters to the encounter with server-side initiative rolls.
    /// </summary>
    Task BulkAddOnlinePlayersAsync(int encounterId, IReadOnlyList<int> characterIds, string storyTellerUserId);

    /// <summary>
    /// Adds a player character to an encounter and recalculates the initiative order.
    /// </summary>
    Task<InitiativeEntry> AddCharacterToEncounterAsync(
        int encounterId,
        int characterId,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId);

    /// <summary>
    /// Adds a named NPC to an encounter and recalculates the initiative order.
    /// </summary>
    Task<InitiativeEntry> AddNpcToEncounterAsync(
        int encounterId,
        string npcName,
        int initiativeMod,
        int rollResult,
        string storyTellerUserId);

    /// <summary>
    /// Marks the next unacted participant as having acted. Wraps rounds and increments <see cref="CombatEncounter.CurrentRound"/>.
    /// </summary>
    Task AdvanceTurnAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Closes the encounter.
    /// </summary>
    Task ResolveEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Returns encounters for a campaign (ST-only).
    /// </summary>
    Task<List<CombatEncounter>> GetEncountersAsync(int campaignId, string storyTellerUserId);

    /// <summary>
    /// Returns a single encounter for a campaign member; non-ST views are redacted.
    /// </summary>
    Task<CombatEncounter?> GetEncounterAsync(int encounterId, string userId);

    /// <summary>
    /// Returns the single active encounter for the campaign, if any, for a campaign member.
    /// </summary>
    Task<CombatEncounter?> GetActiveEncounterForCampaignAsync(int campaignId, string userId);

    /// <summary>
    /// Removes a participant from an encounter and recalculates the initiative order.
    /// </summary>
    Task RemoveEntryAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Persists manual initiative reordering from the Storyteller UI.
    /// </summary>
    Task ReorderInitiativeAsync(int encounterId, IReadOnlyList<int> entryIdsInOrder, string storyTellerUserId);

    /// <summary>
    /// Appends one damage box to an NPC initiative entry.
    /// </summary>
    Task ApplyNpcDamageAsync(int entryId, char damageType, string storyTellerUserId);

    /// <summary>
    /// Removes the last damage mark from an NPC track.
    /// </summary>
    Task HealNpcDamageAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Places the current actor on hold and advances to the next combatant.
    /// </summary>
    Task HoldActionAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Releases a held combatant so they act immediately (ST-chosen entry).
    /// </summary>
    Task ReleaseHeldActionAsync(int encounterId, int entryId, string storyTellerUserId);

    /// <summary>
    /// Sets whether players can see the NPC's true name.
    /// </summary>
    Task SetNpcEntryRevealAsync(int entryId, bool revealed, string? maskedDisplayName, string storyTellerUserId);
}
