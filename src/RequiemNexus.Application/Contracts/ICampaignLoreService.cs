using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Campaign lore entries (member-visible chronicle notes).
/// </summary>
public interface ICampaignLoreService
{
    /// <summary>Returns all lore entries for the campaign, visible to all members.</summary>
    Task<List<CampaignLore>> GetLoreAsync(int campaignId);

    /// <summary>Creates a new lore entry. Any campaign member may create lore.</summary>
    Task<CampaignLore> CreateLoreAsync(int campaignId, string title, string body, string authorUserId);

    /// <summary>Updates an existing lore entry. Only the original author may update.</summary>
    Task UpdateLoreAsync(int loreId, string title, string body, string authorUserId);

    /// <summary>Deletes a lore entry. Author or campaign Storyteller may delete.</summary>
    Task DeleteLoreAsync(int loreId, string requestingUserId);
}
