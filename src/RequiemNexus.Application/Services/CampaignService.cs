using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for campaign management. Owns all data access for the Campaign aggregate.
/// Every mutating operation verifies authorisation before persisting.
/// </summary>
public class CampaignService(ApplicationDbContext dbContext, ILogger<CampaignService> logger) : ICampaignService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<CampaignService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<Campaign>> GetCampaignsByUserIdAsync(string userId)
    {
        return await _dbContext.Campaigns
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
        return await _dbContext.Campaigns
            .Include(c => c.StoryTeller)
            .Include(c => c.Characters).ThenInclude(ch => ch.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc />
    public async Task<Campaign> CreateCampaignAsync(string name, string description, string storytellerUserId)
    {
        var campaign = new Campaign
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
        var campaign = await _dbContext.Campaigns
            .Include(c => c.Characters)
            .FirstOrDefaultAsync(c => c.Id == campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        var character = await _dbContext.Characters.FindAsync(characterId)
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
        var campaign = await _dbContext.Campaigns.FindAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        var character = await _dbContext.Characters.FindAsync(characterId)
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
    public bool IsStoryteller(Campaign campaign, string userId)
        => campaign.StoryTellerId == userId;

    /// <inheritdoc />
    public bool IsCampaignMember(Campaign campaign, string userId)
        => campaign.StoryTellerId == userId
        || campaign.Characters.Any(ch => ch.ApplicationUserId == userId);
}
