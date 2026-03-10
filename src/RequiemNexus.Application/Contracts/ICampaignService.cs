using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages campaign lifecycle and membership. All mutating operations verify that the
/// requesting user is authorised (Storyteller or owner) before proceeding.
/// </summary>
public interface ICampaignService
{
    /// <summary>Returns all campaigns the user is involved with — either as Storyteller or via a character they own.</summary>
    Task<List<Campaign>> GetCampaignsByUserIdAsync(string userId);

    /// <summary>
    /// Returns the campaign with its Storyteller and character roster, or <c>null</c> if not found.
    /// Any authenticated user who is a member may call this.
    /// </summary>
    Task<Campaign?> GetCampaignByIdAsync(int id, string userId);

    /// <summary>Creates a new campaign with <paramref name="storytellerUserId"/> as the Storyteller.</summary>
    Task<Campaign> CreateCampaignAsync(string name, string description, string storytellerUserId);

    /// <summary>
    /// Assigns a character to a campaign. The requesting user must be the Storyteller or the character's owner.
    /// </summary>
    Task AddCharacterToCampaignAsync(int campaignId, int characterId, string userId);

    /// <summary>Removes a character from the campaign. Requires Storyteller or character-owner access.</summary>
    Task RemoveCharacterFromCampaignAsync(int campaignId, int characterId, string userId);

    /// <summary>Returns <c>true</c> when <paramref name="userId"/> is the Storyteller of the campaign.</summary>
    bool IsStoryteller(Campaign campaign, string userId);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="userId"/> is either the Storyteller or has at least one
    /// character enrolled in the campaign.
    /// </summary>
    bool IsCampaignMember(Campaign campaign, string userId);
}
