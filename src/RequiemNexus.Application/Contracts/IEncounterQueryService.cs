using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Read-only queries for combat encounters: list, single, and active encounter for a campaign.
/// </summary>
public interface IEncounterQueryService
{
    /// <summary>Returns encounters for a campaign (ST-only).</summary>
    Task<List<CombatEncounter>> GetEncountersAsync(int campaignId, string storyTellerUserId);

    /// <summary>Returns a single encounter for a campaign member; non-ST views are redacted.</summary>
    Task<CombatEncounter?> GetEncounterAsync(int encounterId, string userId);

    /// <summary>Returns the single active encounter for the campaign, if any, for a campaign member.</summary>
    Task<CombatEncounter?> GetActiveEncounterForCampaignAsync(int campaignId, string userId);
}
