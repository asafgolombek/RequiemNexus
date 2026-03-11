using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing user-scoped homebrew Merits.
/// </summary>
public class HomebrewMeritService(ApplicationDbContext dbContext, ILogger<HomebrewMeritService> logger)
    : HomebrewServiceBase<Merit>(dbContext, logger), IHomebrewMeritService
{
    /// <inheritdoc />
    protected override string EntityTypeName => "Merit";

    /// <inheritdoc />
    public async Task<List<Merit>> GetHomebrewMeritsAsync(string userId)
        => await GetAllCoreAsync(userId);

    /// <inheritdoc />
    public async Task<Merit> CreateHomebrewMeritAsync(string name, string description, string validRatings, bool requiresSpecification, string userId)
    {
        Merit merit = new()
        {
            Name = name,
            Description = description,
            ValidRatings = validRatings,
            RequiresSpecification = requiresSpecification,
            IsHomebrew = true,
            HombrewAuthorUserId = userId,
        };

        return await SaveCoreAsync(merit, userId);
    }

    /// <inheritdoc />
    public async Task DeleteHomebrewMeritAsync(int meritId, string userId)
        => await DeleteCoreAsync(meritId, userId);

    /// <inheritdoc />
    protected override DbSet<Merit> GetDbSet() => DbContext.Merits;

    /// <inheritdoc />
    protected override IQueryable<Merit> QueryByOwner(string userId)
        => DbContext.Merits.Where(m => m.IsHomebrew && m.HombrewAuthorUserId == userId);

    /// <inheritdoc />
    protected override bool IsOwnedBy(Merit entity, string userId)
        => entity.IsHomebrew && entity.HombrewAuthorUserId == userId;

    /// <inheritdoc />
    protected override string GetName(Merit entity) => entity.Name;

    /// <inheritdoc />
    protected override int GetId(Merit entity) => entity.Id;
}
