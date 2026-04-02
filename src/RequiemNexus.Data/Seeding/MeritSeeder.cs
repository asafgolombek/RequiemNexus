using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Seeds the official merit catalog and prerequisites, then merges missing rows from JSON for existing databases.
/// </summary>
public sealed class MeritSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 40;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        await SeedMeritsAsync(context, logger);
        await EnsureMissingMeritDefinitionsFromSeedFilesAsync(context, logger);
    }

    private static async Task SeedMeritsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Merits.AnyAsync(m => !m.IsHomebrew))
        {
            return;
        }

        var merits = MeritSeedData.LoadFromDocs(logger);
        await context.Merits.AddRangeAsync(merits);
        await context.SaveChangesAsync();

        var meritIdsByName = (await context.Merits.Where(m => !m.IsHomebrew).ToListAsync())
            .ToDictionary(m => m.Name, m => m.Id, StringComparer.OrdinalIgnoreCase);
        var prereqs = MeritPrerequisiteSeedData.GetPrerequisitesToSeed(meritIdsByName);
        if (prereqs.Count > 0)
        {
            await context.MeritPrerequisites.AddRangeAsync(prereqs);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Adds merit rows from <c>merits.json</c> and <c>loresheetMerits.json</c> that are absent by name (for existing databases).
    /// </summary>
    private static async Task EnsureMissingMeritDefinitionsFromSeedFilesAsync(ApplicationDbContext context, ILogger logger)
    {
        HashSet<string> existingNames = await context.Merits
            .Select(m => m.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);
        var toAdd = new List<Merit>();
        foreach (string file in new[] { "merits.json", "loresheetMerits.json" })
        {
            foreach (Merit m in MeritSeedData.LoadMeritsFromJsonFile(file, logger))
            {
                if (existingNames.Contains(m.Name))
                {
                    continue;
                }

                toAdd.Add(m);
                existingNames.Add(m.Name);
            }
        }

        if (toAdd.Count == 0)
        {
            return;
        }

        await context.Merits.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();

        Dictionary<string, int> meritIdsByName = await context.Merits
            .Where(m => !m.IsHomebrew)
            .ToDictionaryAsync(m => m.Name, m => m.Id, StringComparer.OrdinalIgnoreCase);
        List<MeritPrerequisite> candidatePrereqs = MeritPrerequisiteSeedData.GetPrerequisitesToSeed(meritIdsByName);
        if (candidatePrereqs.Count == 0)
        {
            return;
        }

        var existingKeys = (await context.MeritPrerequisites
                .Select(p => new { p.MeritId, p.PrerequisiteType, p.ReferenceId, p.OrGroupId })
                .ToListAsync())
            .Select(x => (x.MeritId, x.PrerequisiteType, x.ReferenceId, x.OrGroupId))
            .ToHashSet();

        List<MeritPrerequisite> newPrereqs = candidatePrereqs
            .Where(p => !existingKeys.Contains((p.MeritId, p.PrerequisiteType, p.ReferenceId, p.OrGroupId)))
            .ToList();
        if (newPrereqs.Count > 0)
        {
            await context.MeritPrerequisites.AddRangeAsync(newPrereqs);
            await context.SaveChangesAsync();
        }
    }
}
