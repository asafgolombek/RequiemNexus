using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Links covenant definitions to their packaged merits.
/// </summary>
public sealed class CovenantDefinitionMeritSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 70;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        var covenants = await context.CovenantDefinitions.ToListAsync();
        var merits = await context.Merits.ToListAsync();
        var existing = await context.CovenantDefinitionMerits
            .Select(cdm => new { cdm.CovenantDefinitionId, cdm.MeritId })
            .ToListAsync();
        var existingSet = new HashSet<(int, int)>(existing.Select(e => (e.CovenantDefinitionId, e.MeritId)));

        var covenantByName = covenants.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var meritByName = merits.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);

        var links = new List<(string Covenant, string Merit)>
        {
            ("The Carthian Movement", "Status (Carthian)"),
            ("The Carthian Movement", "Carthian Pull"),
            ("The Carthian Movement", "Plausible Deniability"),
            ("The Carthian Movement", "Strength of Resolution"),
            ("The Carthian Movement", "Mandate from the Masses"),
            ("The Circle of the Crone", "Status (Crone)"),
            ("The Circle of the Crone", "Altar"),
            ("The Circle of the Crone", "The Mother-Daughter Bond"),
            ("The Circle of the Crone", "Undead Menses"),
            ("The Invictus", "Status (Invictus)"),
            ("The Invictus", "Attaché"),
            ("The Invictus", "Friends in High Places"),
            ("The Invictus", "Invested"),
            ("The Invictus", "Notary"),
            ("The Invictus", "Oath of Fealty"),
            ("The Invictus", "Oath of Penance"),
            ("The Invictus", "Oath of Serfdom"),
            ("The Lancea et Sanctum", "Status (Lancea)"),
            ("The Lancea et Sanctum", "Anointed"),
            ("The Ordo Dracul", "Status (Ordo)"),
            ("The Ordo Dracul", "Sworn"),
        };

        var toAdd = new List<CovenantDefinitionMerit>();
        foreach (var (covenantName, meritName) in links)
        {
            if (!covenantByName.TryGetValue(covenantName, out var covenant) ||
                !meritByName.TryGetValue(meritName, out var merit))
            {
                continue;
            }

            if (existingSet.Contains((covenant.Id, merit.Id)))
            {
                continue;
            }

            toAdd.Add(new CovenantDefinitionMerit
            {
                CovenantDefinitionId = covenant.Id,
                MeritId = merit.Id,
            });
            existingSet.Add((covenant.Id, merit.Id));
        }

        if (toAdd.Count > 0)
        {
            await context.CovenantDefinitionMerits.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
        }
    }
}
