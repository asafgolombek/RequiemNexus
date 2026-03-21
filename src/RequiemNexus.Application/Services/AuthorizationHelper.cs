using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public class AuthorizationHelper(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ILogger<AuthorizationHelper> logger) : IAuthorizationHelper
{
    /// <inheritdoc />
    public async Task RequireStorytellerAsync(int campaignId, string userId, string operationName = "perform this action")
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        bool isSt = await dbContext.Campaigns
            .AnyAsync(c => c.Id == campaignId && c.StoryTellerId == userId);

        if (!isSt)
        {
            logger.LogWarning(
                "Unauthorized attempt to {OperationName} on campaign {CampaignId}",
                LogSanitizer.ForLog(operationName),
                campaignId);

            throw new UnauthorizedAccessException(
                $"Only the campaign Storyteller may {operationName}.");
        }
    }

    /// <inheritdoc />
    public async Task RequireCharacterAccessAsync(int characterId, string userId, string operationName = "perform this action")
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        // Use navigation in the projection so EF generates one SQL statement (join). A separate
        // IQueryable inside Select (e.g. dbContext.Campaigns.Any(...)) can trigger a second active
        // operation on this DbContext and throw InvalidOperationException during execution.
        var access = await dbContext.Characters
            .AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => new
            {
                IsOwner = c.ApplicationUserId == userId,
                IsStoryteller = c.Campaign != null && c.Campaign.StoryTellerId == userId,
            })
            .FirstOrDefaultAsync();

        if (access is null)
        {
            throw new InvalidOperationException($"Character {characterId} not found.");
        }

        if (!access.IsOwner && !access.IsStoryteller)
        {
            logger.LogWarning(
                "Unauthorized attempt to {OperationName} on character {CharacterId}",
                LogSanitizer.ForLog(operationName),
                characterId);

            throw new UnauthorizedAccessException(
                $"Only the character's owner or the campaign Storyteller may {operationName}.");
        }
    }

    /// <inheritdoc />
    public async Task RequireCharacterOwnerAsync(int characterId, string userId, string operationName = "perform this action")
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Character character = await dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.ApplicationUserId != userId)
        {
            logger.LogWarning(
                "Unauthorized attempt to {OperationName} on character {CharacterId}",
                LogSanitizer.ForLog(operationName),
                characterId);

            throw new UnauthorizedAccessException(
                $"Only the character's owner may {operationName}.");
        }
    }

    /// <inheritdoc />
    public async Task RequireCampaignMemberAsync(int campaignId, string userId, string operationName = "access this campaign")
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        await RequireCampaignMemberAsync(dbContext, campaignId, userId, operationName);
    }

    /// <inheritdoc />
    public async Task RequireCampaignMemberAsync(ApplicationDbContext context, int campaignId, string userId, string operationName = "access this campaign")
    {
        bool allowed = await context.Campaigns
            .AnyAsync(c =>
                c.Id == campaignId
                && (c.StoryTellerId == userId || c.Characters.Any(ch => ch.ApplicationUserId == userId)));

        if (!allowed)
        {
            logger.LogWarning(
                "Unauthorized attempt to {OperationName} on campaign {CampaignId}",
                LogSanitizer.ForLog(operationName),
                campaignId);

            throw new UnauthorizedAccessException(
                $"You must be the Storyteller or a player in this campaign to {operationName}.");
        }
    }
}
