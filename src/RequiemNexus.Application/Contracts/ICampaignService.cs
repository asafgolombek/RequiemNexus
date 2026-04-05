using RequiemNexus.Application.DTOs;
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
    /// Returns the campaign with its Storyteller and character roster, or <c>null</c> if the campaign does not exist
    /// or <paramref name="userId"/> is neither the Storyteller nor the owner of a character in the campaign
    /// (aligned with live-session hub membership).
    /// </summary>
    Task<Campaign?> GetCampaignByIdAsync(int id, string userId);

    /// <summary>Creates a new campaign with <paramref name="storytellerUserId"/> as the Storyteller.</summary>
    Task<Campaign> CreateCampaignAsync(string name, string description, string storytellerUserId);

    /// <summary>
    /// Assigns an unassigned character to a campaign. The caller must own the character and be the Storyteller
    /// or already have another character in this campaign (use <see cref="JoinCampaignWithInviteAsync"/> for first join).
    /// </summary>
    Task AddCharacterToCampaignAsync(int campaignId, int characterId, string userId);

    /// <summary>
    /// Storyteller-only: replaces the join invite with a new random token and returns the plaintext token once (store only the hash in the database).
    /// </summary>
    Task<string> RegenerateJoinInviteAsync(int campaignId, string stUserId);

    /// <summary>Storyteller-only: disables player invite links for the campaign.</summary>
    Task ClearJoinInviteAsync(int campaignId, string stUserId);

    /// <summary>
    /// When <paramref name="inviteToken"/> matches the stored hash, returns minimal campaign info for the join UI; otherwise <c>null</c>.
    /// Requires an authenticated <paramref name="userId"/>.
    /// </summary>
    Task<CampaignJoinPreviewDto?> GetJoinPreviewAsync(int campaignId, string inviteToken, string userId);

    /// <summary>
    /// Enrolls the caller's unassigned character using a valid invite token. Verifies ownership and invite hash.
    /// </summary>
    Task JoinCampaignWithInviteAsync(int campaignId, int characterId, string inviteToken, string userId);

    /// <summary>Removes a character from the campaign. Requires Storyteller or character-owner access.</summary>
    Task RemoveCharacterFromCampaignAsync(int campaignId, int characterId, string userId);

    /// <summary>
    /// Deletes the campaign. ST-only. Before deletion, nulls out <c>CampaignId</c> on enrolled characters,
    /// beat/XP ledger rows, public rolls, and scoped character notes so FK constraints allow removal.
    /// </summary>
    Task DeleteCampaignAsync(int campaignId, string storyTellerUserId);

    /// <summary>
    /// Removes the calling player from the campaign by nulling out their enrolled character's <c>CampaignId</c>.
    /// Throws <see cref="InvalidOperationException"/> when the user has no enrolled character or is the Storyteller.
    /// </summary>
    Task LeaveCampaignAsync(int campaignId, string playerUserId);

    /// <summary>Returns all campaigns for which <paramref name="userId"/> is the Storyteller.</summary>
    Task<List<Campaign>> GetStorytoldCampaignsAsync(string userId);

    // Campaign Lore (visible to all members)

    /// <summary>Returns all lore entries for the campaign, visible to all members.</summary>
    Task<List<CampaignLore>> GetLoreAsync(int campaignId);

    /// <summary>Creates a new lore entry. Any campaign member may create lore.</summary>
    Task<CampaignLore> CreateLoreAsync(int campaignId, string title, string body, string authorUserId);

    /// <summary>Updates an existing lore entry. Only the original author may update.</summary>
    Task UpdateLoreAsync(int loreId, string title, string body, string authorUserId);

    /// <summary>Deletes a lore entry. Author or campaign Storyteller may delete.</summary>
    Task DeleteLoreAsync(int loreId, string requestingUserId);

    // Session Prep Notes (Storyteller-only)

    /// <summary>Returns all session-prep notes for the campaign. ST-only.</summary>
    Task<List<SessionPrepNote>> GetSessionPrepNotesAsync(int campaignId, string stUserId);

    /// <summary>Creates a new session-prep note. ST-only.</summary>
    Task<SessionPrepNote> CreateSessionPrepNoteAsync(int campaignId, string title, string body, string stUserId);

    /// <summary>Updates an existing session-prep note. ST-only.</summary>
    Task UpdateSessionPrepNoteAsync(int noteId, string title, string body, string stUserId);

    /// <summary>Deletes a session-prep note. ST-only.</summary>
    Task DeleteSessionPrepNoteAsync(int noteId, string stUserId);

    /// <summary>
    /// Storyteller-only: sets or clears the optional Discord incoming webhook URL used for session presence posts (start/end/join/leave).
    /// </summary>
    /// <param name="campaignId">Campaign id.</param>
    /// <param name="discordWebhookUrl">HTTPS Discord webhook URL, or <c>null</c>/whitespace to disable.</param>
    /// <param name="stUserId">Storyteller user id.</param>
    /// <exception cref="InvalidOperationException">When the URL fails validation.</exception>
    Task SetDiscordWebhookUrlAsync(int campaignId, string? discordWebhookUrl, string stUserId);

    /// <summary>Returns <c>true</c> when <paramref name="userId"/> is the Storyteller of the campaign.</summary>
    bool IsStoryteller(Campaign campaign, string userId);

    /// <summary>
    /// Returns <c>true</c> when <paramref name="userId"/> is either the Storyteller or has at least one
    /// character enrolled in the campaign.
    /// </summary>
    bool IsCampaignMember(Campaign campaign, string userId);
}
