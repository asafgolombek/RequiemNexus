using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Application.Services;

/// <summary>
/// EF-backed lore CRUD and chronicle broadcast when new lore is created.
/// </summary>
public class CampaignLoreService(
    ApplicationDbContext dbContext,
    ISessionService sessionService,
    IAuthorizationHelper authHelper,
    ILogger<CampaignLoreService> logger) : ICampaignLoreService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ISessionService _sessionService = sessionService;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ILogger<CampaignLoreService> _logger = logger;

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
        await _authHelper.RequireCampaignMemberAsync(campaignId, authorUserId, "create lore");

        CampaignLore lore = new()
        {
            CampaignId = campaignId,
            AuthorUserId = authorUserId,
            Title = title,
            Body = body,
        };

        _dbContext.CampaignLore.Add(lore);
        await _dbContext.SaveChangesAsync();

        await _sessionService.BroadcastChronicleUpdateAsync(
            new ChronicleUpdateDto(campaignId, BeatAwardedMessage: $"New Lore Entry: {title}"));

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
}
