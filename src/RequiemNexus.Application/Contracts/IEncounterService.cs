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
    /// Renames a draft encounter in prep. Only the Storyteller may call this.
    /// </summary>
    Task UpdateDraftEncounterNameAsync(int encounterId, string name, string storyTellerUserId);

    /// <summary>
    /// Returns suggested initiative modifier and health for a chronicle NPC, or <c>null</c> if not found.
    /// Caller must be the campaign Storyteller.
    /// </summary>
    Task<ChronicleNpcEncounterPrepDto?> GetChronicleNpcEncounterPrepAsync(int chronicleNpcId, string storyTellerUserId);

    /// <summary>
    /// Adds a draft NPC line from a Danse Macabre chronicle NPC (same campaign as the encounter).
    /// <paramref name="maxVitae"/> is the Kindred blood pool maximum (0 for mortals; ignored when the NPC is not a vampire).
    /// </summary>
    Task<EncounterNpcTemplate> AddNpcTemplateFromChronicleNpcAsync(
        int encounterId,
        int chronicleNpcId,
        int initiativeMod,
        int healthBoxes,
        int maxWillpower,
        int maxVitae,
        bool isRevealed,
        string? defaultMaskedName,
        string storyTellerUserId);

    /// <summary>
    /// Adds an active initiative row from a Danse Macabre chronicle NPC (encounter must be launched and active).
    /// <paramref name="maxVitae"/> is the Kindred blood pool maximum (0 for mortals; ignored when the NPC is not a vampire).
    /// </summary>
    Task<InitiativeEntry> AddNpcToEncounterFromChronicleNpcAsync(
        int encounterId,
        int chronicleNpcId,
        int initiativeMod,
        int rollResult,
        int healthBoxes,
        int maxWillpower,
        int maxVitae,
        string storyTellerUserId);

    /// <summary>
    /// Activates a draft encounter, rolls initiative for all NPC templates, and enforces a single active fight per campaign.
    /// </summary>
    Task LaunchEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Adds an NPC template row to a draft encounter. <paramref name="maxWillpower"/> is applied when the encounter is launched.
    /// </summary>
    Task<EncounterNpcTemplate> AddNpcTemplateAsync(
        int encounterId,
        string name,
        int initiativeMod,
        int healthBoxes,
        int maxWillpower,
        string? notes,
        bool isRevealed,
        string? defaultMaskedName,
        string storyTellerUserId,
        int? chronicleNpcId = null,
        int maxVitae = 0);

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
    /// <param name="encounterId">The encounter to add to.</param>
    /// <param name="npcName">Display name for the NPC row.</param>
    /// <param name="initiativeMod">Wits + Composure (or other ST-chosen modifier).</param>
    /// <param name="rollResult">The d10 roll result (1–10).</param>
    /// <param name="storyTellerUserId">The Storyteller's user id.</param>
    /// <param name="npcHealthBoxes">Maximum health boxes for this NPC (default 7).</param>
    /// <param name="npcMaxWillpower">Maximum willpower dots for this NPC.</param>
    /// <param name="chronicleNpcId">When set, links this row to Danse Macabre (must be unique per encounter).</param>
    /// <param name="npcMaxVitae">Maximum vitae for Kindred NPCs; 0 when not applicable.</param>
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

    /// <summary>
    /// Marks the next unacted participant as having acted. Wraps rounds and increments <see cref="CombatEncounter.CurrentRound"/>.
    /// </summary>
    Task AdvanceTurnAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Closes the encounter.
    /// </summary>
    Task ResolveEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Pauses a running encounter: initiative remains in the database; live session initiative is cleared.
    /// </summary>
    Task PauseEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Resumes a paused encounter and republishes initiative to the session.
    /// </summary>
    Task ResumeEncounterAsync(int encounterId, string storyTellerUserId);

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
    /// Spends one willpower from an NPC initiative row (ST only).
    /// </summary>
    Task SpendNpcWillpowerAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Restores one willpower to an NPC initiative row (ST only).
    /// </summary>
    Task RestoreNpcWillpowerAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Spends one vitae from an NPC initiative row (ST only, Kindred rows only).
    /// </summary>
    Task SpendNpcVitaeAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Restores one vitae to an NPC initiative row (ST only).
    /// </summary>
    Task RestoreNpcVitaeAsync(int entryId, string storyTellerUserId);

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
