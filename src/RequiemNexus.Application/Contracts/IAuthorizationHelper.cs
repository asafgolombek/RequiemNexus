using RequiemNexus.Data;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Centralizes ownership and access-level authorization checks used across Application services.
/// All methods throw <see cref="UnauthorizedAccessException"/> on failure so callers can remain
/// free of conditional authorization logic.
/// </summary>
public interface IAuthorizationHelper
{
    /// <summary>
    /// Throws <see cref="UnauthorizedAccessException"/> unless <paramref name="userId"/>
    /// is the Storyteller of <paramref name="campaignId"/>.
    /// </summary>
    /// <param name="campaignId">The campaign to check.</param>
    /// <param name="userId">The requesting user.</param>
    /// <param name="operationName">Human-readable name for log messages (e.g., "manage encounters").</param>
    Task RequireStorytellerAsync(int campaignId, string userId, string operationName = "perform this action");

    /// <summary>
    /// Throws <see cref="UnauthorizedAccessException"/> unless <paramref name="userId"/>
    /// is the character's owner OR the Storyteller of the character's campaign.
    /// </summary>
    /// <param name="characterId">The character to check.</param>
    /// <param name="userId">The requesting user.</param>
    /// <param name="operationName">Human-readable name for log messages.</param>
    Task RequireCharacterAccessAsync(int characterId, string userId, string operationName = "perform this action");

    /// <summary>
    /// Throws <see cref="UnauthorizedAccessException"/> unless <paramref name="userId"/>
    /// is the character's owner.
    /// </summary>
    /// <param name="characterId">The character to check.</param>
    /// <param name="userId">The requesting user.</param>
    /// <param name="operationName">Human-readable name for log messages.</param>
    Task RequireCharacterOwnerAsync(int characterId, string userId, string operationName = "perform this action");

    /// <summary>
    /// Throws <see cref="UnauthorizedAccessException"/> unless <paramref name="userId"/> is the campaign
    /// Storyteller or owns at least one character in the campaign.
    /// </summary>
    /// <param name="campaignId">The campaign to check.</param>
    /// <param name="userId">The requesting user.</param>
    /// <param name="operationName">Human-readable name for log messages.</param>
    Task RequireCampaignMemberAsync(int campaignId, string userId, string operationName = "access this campaign");

    /// <summary>
    /// Same as <see cref="RequireCampaignMemberAsync(int,string,string)"/>, but runs the membership check on the supplied
    /// <see cref="ApplicationDbContext"/> instance. Use with <c>IDbContextFactory</c> so Blazor Server or parallel reads never
    /// overlap operations on the scoped context.
    /// </summary>
    /// <param name="context">The EF Core context to query (caller owns lifetime).</param>
    /// <param name="campaignId">The campaign to check.</param>
    /// <param name="userId">The requesting user.</param>
    /// <param name="operationName">Human-readable name for log messages.</param>
    Task RequireCampaignMemberAsync(ApplicationDbContext context, int campaignId, string userId, string operationName = "access this campaign");
}
