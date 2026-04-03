using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public sealed class CharacterExportCharacterLoader(ApplicationDbContext dbContext) : ICharacterExportCharacterLoader
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<Character?> LoadOwnedCharacterAsync(int characterId, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Characters
            .Include(c => c.Clan)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
            .Include(c => c.Aspirations)
            .Include(c => c.Banes)
            .Where(c => c.Id == characterId && c.ApplicationUserId == userId)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
