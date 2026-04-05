using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Seeds devotion definitions and merges missing rows from seed JSON.
/// </summary>
public sealed class DevotionSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 100;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        if (!await context.DevotionDefinitions.AnyAsync())
        {
            var disciplines = await context.Disciplines.ToListAsync();
            var devotions = DevotionSeedData.LoadFromDocs(disciplines, logger);
            await context.DevotionDefinitions.AddRangeAsync(devotions);
            await context.SaveChangesAsync();
        }

        await DevotionSeedData.EnsureMissingDefinitionsAsync(context, logger);
    }
}
