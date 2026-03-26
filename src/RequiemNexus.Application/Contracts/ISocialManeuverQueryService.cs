using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Read-only queries for Social maneuvers: campaign list and initiator list.
/// </summary>
public interface ISocialManeuverQueryService
{
    /// <summary>Lists all maneuvers in a campaign. Storyteller-only.</summary>
    Task<IReadOnlyList<SocialManeuver>> ListForCampaignAsync(int campaignId, string storytellerUserId);

    /// <summary>Lists maneuvers initiated by the character. Character owner or Storyteller may read.</summary>
    Task<IReadOnlyList<SocialManeuver>> ListForInitiatorAsync(int characterId, string userId);
}
