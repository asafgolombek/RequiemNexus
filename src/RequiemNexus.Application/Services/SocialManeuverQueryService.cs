using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Read-only queries for Social maneuvers.
/// Mutations are handled by <see cref="SocialManeuveringService"/>.
/// Dice rolls are handled by <see cref="SocialManeuverRollService"/>.
/// </summary>
public class SocialManeuverQueryService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper) : ISocialManeuverQueryService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<IReadOnlyList<SocialManeuver>> ListForCampaignAsync(int campaignId, string storytellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storytellerUserId, "list Social maneuvers");

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        return await db.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .Include(m => m.Clues)
            .Include(m => m.Interceptors)
            .ThenInclude(i => i.InterceptorCharacter)
            .Where(m => m.CampaignId == campaignId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SocialManeuver>> ListForInitiatorAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "view Social maneuvers");

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        return await db.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .Include(m => m.Clues)
            .Include(m => m.Interceptors)
            .ThenInclude(i => i.InterceptorCharacter)
            .Where(m => m.InitiatorCharacterId == characterId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }
}
