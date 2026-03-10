using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application service for character notes. Notes may be player-authored or Storyteller-private.
/// Visibility is enforced at query time: Storyteller-private notes are hidden from the character owner
/// when the requesting user is not the campaign Storyteller.
/// </summary>
public interface ICharacterNoteService
{
    /// <summary>
    /// Returns notes visible to <paramref name="requestingUserId"/> for the given character.
    /// Storyteller-private notes are included only when the requester is the campaign Storyteller.
    /// </summary>
    Task<List<CharacterNote>> GetNotesAsync(int characterId, string requestingUserId);

    /// <summary>Creates a new note. The <paramref name="isStorytellerPrivate"/> flag may only be true
    /// when the requesting user is the campaign Storyteller.</summary>
    Task<CharacterNote> CreateNoteAsync(
        int characterId,
        int? campaignId,
        string title,
        string body,
        bool isStorytellerPrivate,
        string authorUserId);

    /// <summary>Updates an existing note. Only the original author may update.</summary>
    Task UpdateNoteAsync(int noteId, string title, string body, string requestingUserId);

    /// <summary>Deletes a note. Author or campaign Storyteller may delete.</summary>
    Task DeleteNoteAsync(int noteId, string requestingUserId);
}
