using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for character notes. Enforces Storyteller-private visibility rules
/// and author-scoped mutations.
/// </summary>
public class CharacterNoteService(ApplicationDbContext dbContext) : ICharacterNoteService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<List<CharacterNote>> GetNotesAsync(int characterId, string requestingUserId)
    {
        Character? character = await _dbContext.Characters
            .Include(c => c.Campaign)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character == null)
        {
            return [];
        }

        bool isCampaignSt = character.Campaign?.StoryTellerId == requestingUserId;

        return await _dbContext.CharacterNotes
            .Where(n => n.CharacterId == characterId
                        && (!n.IsStorytellerPrivate || isCampaignSt))
            .OrderByDescending(n => n.UpdatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CharacterNote> CreateNoteAsync(
        int characterId,
        int? campaignId,
        string title,
        string body,
        bool isStorytellerPrivate,
        string authorUserId)
    {
        if (isStorytellerPrivate)
        {
            // Only the campaign Storyteller may create private notes.
            if (!campaignId.HasValue)
            {
                throw new InvalidOperationException("Storyteller-private notes require a campaign context.");
            }

            Campaign? campaign = await _dbContext.Campaigns.FindAsync(campaignId.Value);
            if (campaign?.StoryTellerId != authorUserId)
            {
                throw new UnauthorizedAccessException("Only the Storyteller may create private notes.");
            }
        }

        CharacterNote note = new()
        {
            CharacterId = characterId,
            CampaignId = campaignId,
            AuthorUserId = authorUserId,
            IsStorytellerPrivate = isStorytellerPrivate,
            Title = title,
            Body = body,
        };

        _dbContext.CharacterNotes.Add(note);
        await _dbContext.SaveChangesAsync();
        return note;
    }

    /// <inheritdoc />
    public async Task UpdateNoteAsync(int noteId, string title, string body, string requestingUserId)
    {
        CharacterNote note = await _dbContext.CharacterNotes.FindAsync(noteId)
            ?? throw new InvalidOperationException($"Note {noteId} not found.");

        if (note.AuthorUserId != requestingUserId)
        {
            throw new UnauthorizedAccessException("Only the note author may update this note.");
        }

        note.Title = title;
        note.Body = body;
        note.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteNoteAsync(int noteId, string requestingUserId)
    {
        CharacterNote note = await _dbContext.CharacterNotes
            .Include(n => n.Character)
            .ThenInclude(c => c!.Campaign)
            .FirstOrDefaultAsync(n => n.Id == noteId)
            ?? throw new InvalidOperationException($"Note {noteId} not found.");

        bool isAuthor = note.AuthorUserId == requestingUserId;
        bool isCampaignSt = note.Character?.Campaign?.StoryTellerId == requestingUserId;

        if (!isAuthor && !isCampaignSt)
        {
            throw new UnauthorizedAccessException("Only the note author or campaign Storyteller may delete this note.");
        }

        _dbContext.CharacterNotes.Remove(note);
        await _dbContext.SaveChangesAsync();
    }
}
