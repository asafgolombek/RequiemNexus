using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Storyteller-only session prep note CRUD with Masquerade checks via <see cref="IAuthorizationHelper"/>.
/// </summary>
public class CampaignSessionPrepService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ILogger<CampaignSessionPrepService> logger) : ICampaignSessionPrepService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ILogger<CampaignSessionPrepService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<SessionPrepNote>> GetSessionPrepNotesAsync(int campaignId, string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "manage session prep notes");
        return await _dbContext.SessionPrepNotes
            .Where(n => n.CampaignId == campaignId)
            .OrderByDescending(n => n.UpdatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<SessionPrepNote> CreateSessionPrepNoteAsync(int campaignId, string title, string body, string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "manage session prep notes");

        SessionPrepNote note = new()
        {
            CampaignId = campaignId,
            Title = title,
            Body = body,
        };

        _dbContext.SessionPrepNotes.Add(note);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Session prep note '{Title}' created in campaign {CampaignId} by ST {UserId}",
            note.Title,
            campaignId,
            stUserId);

        return note;
    }

    /// <inheritdoc />
    public async Task UpdateSessionPrepNoteAsync(int noteId, string title, string body, string stUserId)
    {
        SessionPrepNote note = await _dbContext.SessionPrepNotes.FindAsync(noteId)
            ?? throw new InvalidOperationException($"Session prep note {noteId} not found.");

        await _authHelper.RequireStorytellerAsync(note.CampaignId, stUserId, "manage session prep notes");

        note.Title = title;
        note.Body = body;
        note.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteSessionPrepNoteAsync(int noteId, string stUserId)
    {
        SessionPrepNote note = await _dbContext.SessionPrepNotes.FindAsync(noteId)
            ?? throw new InvalidOperationException($"Session prep note {noteId} not found.");

        await _authHelper.RequireStorytellerAsync(note.CampaignId, stUserId, "manage session prep notes");

        _dbContext.SessionPrepNotes.Remove(note);
        await _dbContext.SaveChangesAsync();
    }
}
