using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for campaign management. Owns all data access for the Campaign aggregate.
/// Every mutating operation verifies authorisation before persisting.
/// </summary>
public class CampaignService(
    ApplicationDbContext dbContext,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<CampaignService> logger,
    IAuthorizationHelper authHelper,
    ISessionService sessionService) : ICampaignService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<CampaignService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<List<Campaign>> GetCampaignsByUserIdAsync(string userId)
    {
        await using ApplicationDbContext ctx = _dbContextFactory.CreateDbContext();
        return await ctx.Campaigns
            .Include(c => c.Characters)
            .Include(c => c.StoryTeller)
            .Where(c => c.StoryTellerId == userId
                     || c.Characters.Any(ch => ch.ApplicationUserId == userId))
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Campaign?> GetCampaignByIdAsync(int id, string userId)
    {
        await using ApplicationDbContext ctx = _dbContextFactory.CreateDbContext();
        return await ctx.Campaigns
            .Include(c => c.StoryTeller)
            .Include(c => c.Characters).ThenInclude(ch => ch.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc />
    public async Task<Campaign> CreateCampaignAsync(string name, string description, string storytellerUserId)
    {
        Campaign campaign = new()
        {
            Name = name,
            Description = description,
            StoryTellerId = storytellerUserId,
        };

        _dbContext.Campaigns.Add(campaign);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Campaign '{Name}' (Id={CampaignId}) created by StoryTeller {UserId}",
            campaign.Name,
            campaign.Id,
            storytellerUserId);

        return campaign;
    }

    /// <inheritdoc />
    public async Task AddCharacterToCampaignAsync(int campaignId, int characterId, string userId)
    {
        Campaign campaign = await _dbContext.Campaigns
            .Include(c => c.Characters)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        // Only the character's own player may self-enrol.
        // The Storyteller manages the roster by sharing the campaign URL; they do not add characters on behalf of players.
        bool isOwner = character.ApplicationUserId == userId;

        if (!isOwner)
        {
            _logger.LogWarning(
                "Unauthorized attempt to add character {CharacterId} to campaign {CampaignId} by user {UserId}",
                characterId,
                campaignId,
                userId);
            throw new UnauthorizedAccessException("Only the character's owner may add it to a campaign.");
        }

        character.CampaignId = campaignId;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Character {CharacterId} added to campaign {CampaignId} by user {UserId}",
            characterId,
            campaignId,
            userId);
    }

    /// <inheritdoc />
    public async Task RemoveCharacterFromCampaignAsync(int campaignId, int characterId, string userId)
    {
        Campaign campaign = await _dbContext.Campaigns.FindAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        bool isStoryteller = campaign.StoryTellerId == userId;
        bool isOwner = character.ApplicationUserId == userId;

        if (!isStoryteller && !isOwner)
        {
            _logger.LogWarning(
                "Unauthorized attempt to remove character {CharacterId} from campaign {CampaignId} by user {UserId}",
                characterId,
                campaignId,
                userId);
            throw new UnauthorizedAccessException("Only the Storyteller or the character owner may remove a character from this campaign.");
        }

        character.CampaignId = null;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Character {CharacterId} removed from campaign {CampaignId} by user {UserId}",
            characterId,
            campaignId,
            userId);
    }

    /// <inheritdoc />
    public async Task DeleteCampaignAsync(int campaignId, string storyTellerUserId)
    {
        Campaign campaign = await _dbContext.Campaigns
            .Include(c => c.Characters)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        if (campaign.StoryTellerId != storyTellerUserId)
        {
            _logger.LogWarning(
                "Unauthorized attempt to delete campaign {CampaignId} by user {UserId}",
                campaignId,
                storyTellerUserId);
            throw new UnauthorizedAccessException("Only the Storyteller may delete this campaign.");
        }

        foreach (Character character in campaign.Characters)
        {
            character.CampaignId = null;
        }

        _dbContext.Campaigns.Remove(campaign);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Campaign '{Name}' (Id={CampaignId}) deleted by Storyteller {UserId}",
            campaign.Name,
            campaignId,
            storyTellerUserId);
    }

    /// <inheritdoc />
    public async Task LeaveCampaignAsync(int campaignId, string playerUserId)
    {
        Campaign campaign = await _dbContext.Campaigns
            .Include(c => c.Characters)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        if (campaign.StoryTellerId == playerUserId)
        {
            throw new InvalidOperationException("The Storyteller cannot leave a campaign. Use DeleteCampaignAsync instead.");
        }

        Character character = campaign.Characters.FirstOrDefault(ch => ch.ApplicationUserId == playerUserId)
            ?? throw new InvalidOperationException("You have no enrolled character in this campaign.");

        character.CampaignId = null;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Player {UserId} left campaign {CampaignId} (character {CharacterId} removed)",
            playerUserId,
            campaignId,
            character.Id);
    }

    /// <inheritdoc />
    public async Task<List<Campaign>> GetStorytoldCampaignsAsync(string userId)
    {
        return await _dbContext.Campaigns
            .Include(c => c.Characters)
            .Where(c => c.StoryTellerId == userId)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<CampaignLore>> GetLoreAsync(int campaignId)
    {
        return await _dbContext.CampaignLore
            .Where(l => l.CampaignId == campaignId)
            .OrderByDescending(l => l.UpdatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CampaignLore> CreateLoreAsync(int campaignId, string title, string body, string authorUserId)
    {
        CampaignLore lore = new()
        {
            CampaignId = campaignId,
            AuthorUserId = authorUserId,
            Title = title,
            Body = body,
        };

        _dbContext.CampaignLore.Add(lore);
        await _dbContext.SaveChangesAsync();

        await sessionService.BroadcastChronicleUpdateAsync(new ChronicleUpdateDto(campaignId, BeatAwardedMessage: $"New Lore Entry: {title}"));

        _logger.LogInformation(
            "Lore entry '{Title}' created in campaign {CampaignId} by {UserId}",
            lore.Title,
            campaignId,
            authorUserId);

        return lore;
    }

    /// <inheritdoc />
    public async Task UpdateLoreAsync(int loreId, string title, string body, string authorUserId)
    {
        CampaignLore lore = await _dbContext.CampaignLore.FindAsync(loreId)
            ?? throw new InvalidOperationException($"Lore {loreId} not found.");

        if (lore.AuthorUserId != authorUserId)
        {
            throw new UnauthorizedAccessException("Only the lore author may update this entry.");
        }

        lore.Title = title;
        lore.Body = body;
        lore.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteLoreAsync(int loreId, string requestingUserId)
    {
        CampaignLore lore = await _dbContext.CampaignLore
            .Include(l => l.Campaign)
            .FirstOrDefaultAsync(l => l.Id == loreId)
            ?? throw new InvalidOperationException($"Lore {loreId} not found.");

        bool isAuthor = lore.AuthorUserId == requestingUserId;
        bool isSt = lore.Campaign?.StoryTellerId == requestingUserId;

        if (!isAuthor && !isSt)
        {
            throw new UnauthorizedAccessException("Only the author or Storyteller may delete this lore entry.");
        }

        _dbContext.CampaignLore.Remove(lore);
        await _dbContext.SaveChangesAsync();
    }

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

    /// <inheritdoc />
    public bool IsStoryteller(Campaign campaign, string userId)
        => campaign.StoryTellerId == userId;

    /// <inheritdoc />
    public bool IsCampaignMember(Campaign campaign, string userId)
        => campaign.StoryTellerId == userId
        || campaign.Characters.Any(ch => ch.ApplicationUserId == userId);
}
