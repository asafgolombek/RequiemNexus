using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Seeds covenant definitions when absent.
/// </summary>
public sealed class CovenantSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 60;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.CovenantDefinitions.AnyAsync())
        {
            return;
        }

        var covenants = CovenantSeedData.LoadFromDocs(logger);
        await context.CovenantDefinitions.AddRangeAsync(covenants);
        await context.SaveChangesAsync();
    }
}
