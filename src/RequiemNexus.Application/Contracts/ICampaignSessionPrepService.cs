using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Storyteller-only session preparation notes for a campaign.
/// </summary>
public interface ICampaignSessionPrepService
{
    /// <summary>Returns all session-prep notes for the campaign. ST-only.</summary>
    Task<List<SessionPrepNote>> GetSessionPrepNotesAsync(int campaignId, string stUserId);

    /// <summary>Creates a new session-prep note. ST-only.</summary>
    Task<SessionPrepNote> CreateSessionPrepNoteAsync(int campaignId, string title, string body, string stUserId);

    /// <summary>Updates an existing session-prep note. ST-only.</summary>
    Task UpdateSessionPrepNoteAsync(int noteId, string title, string body, string stUserId);

    /// <summary>Deletes a session-prep note. ST-only.</summary>
    Task DeleteSessionPrepNoteAsync(int noteId, string stUserId);
}
