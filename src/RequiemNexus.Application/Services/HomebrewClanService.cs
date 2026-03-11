using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing user-scoped homebrew Clans and Bloodlines.
/// </summary>
public class HomebrewClanService(ApplicationDbContext dbContext, ILogger<HomebrewClanService> logger)
    : HomebrewServiceBase<Clan>(dbContext, logger), IHomebrewClanService
{
    /// <inheritdoc />
    protected override string EntityTypeName => "Clan";

    /// <inheritdoc />
    public async Task<List<Clan>> GetHomebrewClansAsync(string userId)
        => await GetAllCoreAsync(userId);

    /// <inheritdoc />
    public async Task<Clan> CreateHomebrewClanAsync(string name, string description, string userId)
    {
        Clan clan = new()
        {
            Name = name,
            Description = description,
            IsHomebrew = true,
            HombrewAuthorUserId = userId,
        };

        return await SaveCoreAsync(clan, userId);
    }

    /// <inheritdoc />
    public async Task DeleteHomebrewClanAsync(int clanId, string userId)
        => await DeleteCoreAsync(clanId, userId);

    /// <inheritdoc />
    protected override DbSet<Clan> GetDbSet() => DbContext.Clans;

    /// <inheritdoc />
    protected override IQueryable<Clan> QueryByOwner(string userId)
        => DbContext.Clans.Where(c => c.IsHomebrew && c.HombrewAuthorUserId == userId);

    /// <inheritdoc />
    protected override bool IsOwnedBy(Clan entity, string userId)
        => entity.IsHomebrew && entity.HombrewAuthorUserId == userId;

    /// <inheritdoc />
    protected override string GetName(Clan entity) => entity.Name;

    /// <inheritdoc />
    protected override int GetId(Clan entity) => entity.Id;
}
