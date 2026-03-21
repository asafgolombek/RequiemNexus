using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// CRUD for reusable encounter templates within a campaign.
/// </summary>
public interface IEncounterTemplateService
{
    /// <summary>
    /// Creates an empty template shell.
    /// </summary>
    Task<EncounterTemplate> CreateTemplateAsync(int campaignId, string name, string storyTellerUserId);

    /// <summary>
    /// Adds an NPC line to a template.
    /// </summary>
    Task AddNpcToTemplateAsync(
        int templateId,
        string name,
        int initiativeMod,
        int healthBoxes,
        int maxWillpower,
        bool isRevealedByDefault,
        string? defaultMaskedName,
        string storyTellerUserId);

    /// <summary>
    /// Returns templates for the campaign (ST-only).
    /// </summary>
    Task<List<EncounterTemplate>> GetTemplatesAsync(int campaignId, string storyTellerUserId);

    /// <summary>
    /// Copies template NPC rows into a new draft encounter.
    /// </summary>
    Task<CombatEncounter> CreateDraftEncounterFromTemplateAsync(
        int templateId,
        string encounterName,
        string storyTellerUserId);

    /// <summary>
    /// Deletes a template.
    /// </summary>
    Task DeleteTemplateAsync(int templateId, string storyTellerUserId);
}
