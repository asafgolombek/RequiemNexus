using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Seeds bloodline definitions when absent.
/// </summary>
public sealed class BloodlineSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 80;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.BloodlineDefinitions.AnyAsync())
        {
            return;
        }

        var clans = await context.Clans.ToListAsync();
        var disciplines = await context.Disciplines.ToListAsync();
        var bloodlines = BloodlineSeedData.LoadFromDocs(clans, disciplines, logger);
        await context.BloodlineDefinitions.AddRangeAsync(bloodlines);
        await context.SaveChangesAsync();
    }
}
