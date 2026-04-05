using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Ensures scale and coil definitions exist for every entry in coil seed data.
/// </summary>
public sealed class CoilSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 130;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        var entries = CoilSeedData.LoadFromDocs(logger);
        List<string> existingNames = await context.ScaleDefinitions
            .AsNoTracking()
            .Select(s => s.Name)
            .ToListAsync();
        HashSet<string> existingScaleNames = existingNames.ToHashSet(StringComparer.Ordinal);

        foreach ((ScaleDefinition scale, List<CoilDefinition> coils) in entries)
        {
            if (existingScaleNames.Contains(scale.Name))
            {
                continue;
            }

            context.ScaleDefinitions.Add(scale);
            await context.SaveChangesAsync();
            existingScaleNames.Add(scale.Name);

            List<CoilDefinition> orderedCoils = coils.OrderBy(c => c.Level).ToList();
            foreach (CoilDefinition coil in orderedCoils)
            {
                coil.ScaleId = scale.Id;
            }

            await context.CoilDefinitions.AddRangeAsync(orderedCoils);
            await context.SaveChangesAsync();
        }
    }
}
