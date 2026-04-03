using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// EF-backed read queries for characters. Does not perform authorization; callers on <see cref="ICharacterService"/> gate access.
/// </summary>
public class CharacterQueryService(
    ApplicationDbContext dbContext,
    IDbContextFactory<ApplicationDbContext> dbContextFactory) : ICharacterQueryService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;

    /// <inheritdoc />
    public async Task<List<Character>> GetCharactersByUserIdAsync(string userId)
    {
        await using ApplicationDbContext ctx = await _dbContextFactory.CreateDbContextAsync();
        return await ctx.Characters
            .Include(c => c.Clan)
            .Where(c => c.ApplicationUserId == userId && !c.IsArchived)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<Character>> GetArchivedCharactersAsync(string userId)
    {
        await using ApplicationDbContext ctx = await _dbContextFactory.CreateDbContextAsync();
        return await ctx.Characters
            .Include(c => c.Clan)
            .Where(c => c.ApplicationUserId == userId && c.IsArchived)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(Character Character, bool IsOwner)?> GetCharacterWithAccessCheckAsync(
        int characterId,
        string requestingUserId)
    {
        Character? character = await _dbContext.Characters
            .IncludeCharacterAccessSnapshotGraph()
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character == null)
        {
            return null;
        }

        if (character.ApplicationUserId == requestingUserId)
        {
            return (character, true);
        }

        if (character.CampaignId.HasValue)
        {
            bool isMember = await _dbContext.Campaigns
                .AnyAsync(c => c.Id == character.CampaignId
                    && (c.StoryTellerId == requestingUserId
                        || c.Characters.Any(ch => ch.ApplicationUserId == requestingUserId)));

            if (isMember)
            {
                return (character, false);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CampaignKindredTargetDto>> GetCampaignKindredTargetsForRitesAsync(int characterId)
    {
        await using ApplicationDbContext ctx = await _dbContextFactory.CreateDbContextAsync();
        Character? ch = await ctx.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == characterId);
        if (ch?.CampaignId is not int campId)
        {
            return [];
        }

        return await ctx.Characters
            .AsNoTracking()
            .Where(c => c.CampaignId == campId && c.Id != characterId && c.CreatureType == CreatureType.Vampire)
            .OrderBy(c => c.Name)
            .Select(c => new CampaignKindredTargetDto(c.Id, c.Name))
            .ToListAsync();
    }
}
