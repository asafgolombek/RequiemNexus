using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Security;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

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
    ICampaignLoreService loreService,
    ICampaignSessionPrepService sessionPrepService) : ICampaignService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly ILogger<CampaignService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ICampaignLoreService _loreService = loreService;
    private readonly ICampaignSessionPrepService _sessionPrepService = sessionPrepService;

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
        await using ApplicationDbContext ctx = await _dbContextFactory.CreateDbContextAsync();

        // Membership folded into the main query (same predicate as <see cref="IAuthorizationHelper.RequireCampaignMemberAsync"/>).
        // Unauthorized callers get null — no campaign row is materialized.
        // Do not Include StoryTeller or Character.User on this graph: with AsSplitQuery(), SQLite/InMemory and some
        // providers treat missing AspNetUsers principals like inner joins and drop the entire campaign row
        // (see CampaignServiceTests — seeds campaigns without user rows).
        Campaign? campaign = await ctx.Campaigns
            .Include(c => c.Characters).ThenInclude(ch => ch.Clan)
            .AsSplitQuery()
            .AsNoTracking()
            .Where(c =>
                c.Id == id
                && (c.StoryTellerId == userId || c.Characters.Any(ch => ch.ApplicationUserId == userId)))
            .FirstOrDefaultAsync();

        if (campaign is null)
        {
            return null;
        }

        HashSet<string> userIds = new(
            campaign.Characters.Select(ch => ch.ApplicationUserId).Append(campaign.StoryTellerId));

        Dictionary<string, ApplicationUser> users = await ctx.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        if (users.TryGetValue(campaign.StoryTellerId, out ApplicationUser? storyteller))
        {
            campaign.StoryTeller = storyteller;
        }

        foreach (Character character in campaign.Characters)
        {
            if (users.TryGetValue(character.ApplicationUserId, out ApplicationUser? player))
            {
                character.User = player;
            }
        }

        return campaign;
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
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "add it to a campaign");

        Campaign campaign = await _dbContext.Campaigns
            .Include(c => c.Characters)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        if (!CallerMayEnrollWithoutInvite(campaign, userId))
        {
            _logger.LogWarning(
                "User {UserId} attempted AddCharacterToCampaign without ST/membership on campaign {CampaignId}",
                userId,
                campaignId);

            throw new UnauthorizedAccessException(
                "Only the Storyteller or a player already in this campaign may add a character this way. Use your invite link to join with your first character.");
        }

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId != null)
        {
            throw new InvalidOperationException("That character is already assigned to a campaign.");
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
    public async Task<string> RegenerateJoinInviteAsync(int campaignId, string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "manage join invites");

        Campaign campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        string token = CampaignInviteTokenHasher.GenerateToken();
        campaign.InviteTokenHash = CampaignInviteTokenHasher.Hash(token);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Join invite regenerated for campaign {CampaignId} by ST {UserId}", campaignId, stUserId);

        return token;
    }

    /// <inheritdoc />
    public async Task ClearJoinInviteAsync(int campaignId, string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "manage join invites");

        Campaign campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        campaign.InviteTokenHash = null;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Join invite cleared for campaign {CampaignId} by ST {UserId}", campaignId, stUserId);
    }

    /// <inheritdoc />
    public async Task<CampaignJoinPreviewDto?> GetJoinPreviewAsync(int campaignId, string inviteToken, string userId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(inviteToken))
        {
            return null;
        }

        await using ApplicationDbContext ctx = await _dbContextFactory.CreateDbContextAsync();
        var row = await ctx.Campaigns.AsNoTracking()
            .Where(c => c.Id == campaignId)
            .Select(c => new { c.Name, c.InviteTokenHash })
            .FirstOrDefaultAsync();

        if (row is null || string.IsNullOrEmpty(row.InviteTokenHash))
        {
            return null;
        }

        if (!CampaignInviteTokenHasher.Verify(row.InviteTokenHash, inviteToken))
        {
            return null;
        }

        return new CampaignJoinPreviewDto(campaignId, row.Name);
    }

    /// <inheritdoc />
    public async Task JoinCampaignWithInviteAsync(int campaignId, int characterId, string inviteToken, string userId)
    {
        if (string.IsNullOrWhiteSpace(inviteToken))
        {
            throw new UnauthorizedAccessException("A valid invite link is required to join this campaign.");
        }

        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "join this campaign");

        string? storedHash = await _dbContext.Campaigns.AsNoTracking()
            .Where(c => c.Id == campaignId)
            .Select(c => c.InviteTokenHash)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(storedHash) || !CampaignInviteTokenHasher.Verify(storedHash, inviteToken))
        {
            _logger.LogWarning("Invalid join invite attempt for campaign {CampaignId} by user {UserId}", campaignId, userId);
            throw new UnauthorizedAccessException("This invite link is invalid or has been disabled.");
        }

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId != null)
        {
            throw new InvalidOperationException("That character is already assigned to a campaign.");
        }

        character.CampaignId = campaignId;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} joined campaign {CampaignId} with character {CharacterId} via invite",
            userId,
            campaignId,
            characterId);
    }

    /// <inheritdoc />
    public async Task RemoveCharacterFromCampaignAsync(int campaignId, int characterId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "remove a character from this campaign");

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

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
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "delete this campaign");

        Campaign campaign = await _dbContext.Campaigns
            .Include(c => c.Characters)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        foreach (Character character in campaign.Characters)
        {
            character.CampaignId = null;
        }

        // Optional CampaignId FKs are not configured with database SetNull/Cascade; SQLite/PostgreSQL keep RESTRICT
        // and block campaign deletion until these rows are cleared.
        await _dbContext.BeatLedger
            .Where(e => e.CampaignId == campaignId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.CampaignId, (int?)null));
        await _dbContext.XpLedger
            .Where(e => e.CampaignId == campaignId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.CampaignId, (int?)null));
        await _dbContext.PublicRolls
            .Where(r => r.CampaignId == campaignId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.CampaignId, (int?)null));
        await _dbContext.CharacterNotes
            .Where(n => n.CampaignId == campaignId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.CampaignId, (int?)null));

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
    public Task<List<CampaignLore>> GetLoreAsync(int campaignId) => _loreService.GetLoreAsync(campaignId);

    /// <inheritdoc />
    public Task<CampaignLore> CreateLoreAsync(int campaignId, string title, string body, string authorUserId) =>
        _loreService.CreateLoreAsync(campaignId, title, body, authorUserId);

    /// <inheritdoc />
    public Task UpdateLoreAsync(int loreId, string title, string body, string authorUserId) =>
        _loreService.UpdateLoreAsync(loreId, title, body, authorUserId);

    /// <inheritdoc />
    public Task DeleteLoreAsync(int loreId, string requestingUserId) =>
        _loreService.DeleteLoreAsync(loreId, requestingUserId);

    /// <inheritdoc />
    public Task<List<SessionPrepNote>> GetSessionPrepNotesAsync(int campaignId, string stUserId) =>
        _sessionPrepService.GetSessionPrepNotesAsync(campaignId, stUserId);

    /// <inheritdoc />
    public Task<SessionPrepNote> CreateSessionPrepNoteAsync(int campaignId, string title, string body, string stUserId) =>
        _sessionPrepService.CreateSessionPrepNoteAsync(campaignId, title, body, stUserId);

    /// <inheritdoc />
    public Task UpdateSessionPrepNoteAsync(int noteId, string title, string body, string stUserId) =>
        _sessionPrepService.UpdateSessionPrepNoteAsync(noteId, title, body, stUserId);

    /// <inheritdoc />
    public Task DeleteSessionPrepNoteAsync(int noteId, string stUserId) =>
        _sessionPrepService.DeleteSessionPrepNoteAsync(noteId, stUserId);

    /// <inheritdoc />
    public async Task SetDiscordWebhookUrlAsync(int campaignId, string? discordWebhookUrl, string stUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, stUserId, "configure the Discord session webhook");

        Result<string?> validation = DiscordIncomingWebhookValidator.Validate(discordWebhookUrl);
        if (!validation.IsSuccess)
        {
            throw new InvalidOperationException(validation.Error);
        }

        Campaign? campaign = await _dbContext.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId);
        if (campaign is null)
        {
            throw new InvalidOperationException($"Campaign {campaignId} not found.");
        }

        campaign.DiscordWebhookUrl = validation.Value;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Discord session webhook updated for campaign {CampaignId} by storyteller {UserId}",
            campaignId,
            stUserId);
    }

    /// <inheritdoc />
    public bool IsStoryteller(Campaign campaign, string userId)
        => campaign.StoryTellerId == userId;

    /// <inheritdoc />
    public bool IsCampaignMember(Campaign campaign, string userId)
        => campaign.StoryTellerId == userId
        || campaign.Characters.Any(ch => ch.ApplicationUserId == userId);

    private static bool CallerMayEnrollWithoutInvite(Campaign campaign, string userId)
        => campaign.StoryTellerId == userId
        || campaign.Characters.Any(ch => ch.ApplicationUserId == userId);
}
