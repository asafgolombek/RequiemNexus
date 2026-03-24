using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages draft encounters and NPC templates before an encounter is launched.
/// </summary>
public interface IEncounterPrepService
{
    /// <summary>
    /// Creates a draft encounter (prep). Launch activates it when the table is ready.
    /// </summary>
    /// <param name="campaignId">Campaign to attach the draft to.</param>
    /// <param name="name">Encounter display name.</param>
    /// <param name="storyTellerUserId">Authenticated Storyteller user id.</param>
    /// <param name="prepNotes">Optional ST-only prep notes (trimmed, max 4000 characters).</param>
    Task<CombatEncounter> CreateDraftEncounterAsync(
        int campaignId,
        string name,
        string storyTellerUserId,
        string? prepNotes = null);

    /// <summary>
    /// Renames a draft encounter in prep. Only the Storyteller may call this.
    /// </summary>
    Task UpdateDraftEncounterNameAsync(int encounterId, string name, string storyTellerUserId);

    /// <summary>
    /// Updates prep notes on a draft encounter. Only the Storyteller may call this.
    /// </summary>
    /// <param name="encounterId">Draft encounter id.</param>
    /// <param name="prepNotes">Notes text, or null/whitespace to clear.</param>
    /// <param name="storyTellerUserId">Authenticated Storyteller user id.</param>
    Task UpdateDraftEncounterPrepNotesAsync(int encounterId, string? prepNotes, string storyTellerUserId);

    /// <summary>
    /// Deletes a draft encounter and its NPC template rows. Only the Storyteller may call this.
    /// </summary>
    /// <param name="encounterId">Draft encounter id.</param>
    /// <param name="storyTellerUserId">Authenticated Storyteller user id.</param>
    Task DeleteDraftEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Returns suggested initiative modifier and health for a chronicle NPC, or <c>null</c> if not found.
    /// Caller must be the campaign Storyteller.
    /// </summary>
    Task<ChronicleNpcEncounterPrepDto?> GetChronicleNpcEncounterPrepAsync(int chronicleNpcId, string storyTellerUserId);

    /// <summary>
    /// Adds a draft NPC line from a Danse Macabre chronicle NPC (same campaign as the encounter).
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
    /// Adds an NPC template row to a draft encounter.
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
}
