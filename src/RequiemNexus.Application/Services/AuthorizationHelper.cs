using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public class AuthorizationHelper(ApplicationDbContext dbContext, ILogger<AuthorizationHelper> logger) : IAuthorizationHelper
{
    /// <inheritdoc />
    public async Task RequireStorytellerAsync(int campaignId, string userId, string operationName = "perform this action")
    {
        bool isSt = await dbContext.Campaigns
            .AnyAsync(c => c.Id == campaignId && c.StoryTellerId == userId);

        if (!isSt)
        {
            logger.LogWarning(
                "Unauthorized attempt to {OperationName} on campaign {CampaignId} by user {UserId}",
                operationName,
                campaignId,
                userId);

            throw new UnauthorizedAccessException(
                $"Only the campaign Storyteller may {operationName}.");
        }
    }

    /// <inheritdoc />
    public async Task RequireCharacterAccessAsync(int characterId, string userId, string operationName = "perform this action")
    {
        Character character = await dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        bool isOwner = character.ApplicationUserId == userId;
        bool isStoryteller = character.CampaignId.HasValue
            && await dbContext.Campaigns.AnyAsync(
                c => c.Id == character.CampaignId && c.StoryTellerId == userId);

        if (!isOwner && !isStoryteller)
        {
            logger.LogWarning(
                "Unauthorized attempt to {OperationName} on character {CharacterId} by user {UserId}",
                operationName,
                characterId,
                userId);

            throw new UnauthorizedAccessException(
                $"Only the character's owner or the campaign Storyteller may {operationName}.");
        }
    }

    /// <inheritdoc />
    public async Task RequireCharacterOwnerAsync(int characterId, string userId, string operationName = "perform this action")
    {
        Character character = await dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.ApplicationUserId != userId)
        {
            logger.LogWarning(
                "Unauthorized attempt to {OperationName} on character {CharacterId} by user {UserId}",
                operationName,
                characterId,
                userId);

            throw new UnauthorizedAccessException(
                $"Only the character's owner may {operationName}.");
        }
    }

    /// <inheritdoc />
    public async Task RequireCampaignMemberAsync(int campaignId, string userId, string operationName = "access this campaign")
    {
        bool allowed = await dbContext.Campaigns
            .AnyAsync(c =>
                c.Id == campaignId
                && (c.StoryTellerId == userId || c.Characters.Any(ch => ch.ApplicationUserId == userId)));

        if (!allowed)
        {
            logger.LogWarning(
                "Unauthorized attempt to {OperationName} on campaign {CampaignId} by user {UserId}",
                operationName,
                campaignId,
                userId);

            throw new UnauthorizedAccessException(
                $"You must be the Storyteller or a player in this campaign to {operationName}.");
        }
    }
}
