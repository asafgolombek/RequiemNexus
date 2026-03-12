using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing user-scoped homebrew Disciplines.
/// </summary>
public class HomebrewDisciplineService(ApplicationDbContext dbContext, ILogger<HomebrewDisciplineService> logger)
    : HomebrewServiceBase<Discipline>(dbContext, logger), IHomebrewDisciplineService
{
    /// <inheritdoc />
    protected override string EntityTypeName => "Discipline";

    /// <inheritdoc />
    public async Task<List<Discipline>> GetHomebrewDisciplinesAsync(string userId)
        => await GetAllCoreAsync(userId);

    /// <inheritdoc />
    public async Task<Discipline> CreateHomebrewDisciplineAsync(string name, string description, string userId)
    {
        Discipline discipline = new()
        {
            Name = name,
            Description = description,
            IsHomebrew = true,
            HombrewAuthorUserId = userId,
        };

        return await SaveCoreAsync(discipline, userId);
    }

    /// <inheritdoc />
    public async Task DeleteHomebrewDisciplineAsync(int disciplineId, string userId)
        => await DeleteCoreAsync(disciplineId, userId);

    /// <inheritdoc />
    protected override DbSet<Discipline> GetDbSet() => DbContext.Disciplines;

    /// <inheritdoc />
    protected override IQueryable<Discipline> QueryByOwner(string userId)
        => DbContext.Disciplines.Where(d => d.IsHomebrew && d.HombrewAuthorUserId == userId);

    /// <inheritdoc />
    protected override bool IsOwnedBy(Discipline entity, string userId)
        => entity.IsHomebrew && entity.HombrewAuthorUserId == userId;

    /// <inheritdoc />
    protected override string GetName(Discipline entity) => entity.Name;

    /// <inheritdoc />
    protected override int GetId(Discipline entity) => entity.Id;
}
