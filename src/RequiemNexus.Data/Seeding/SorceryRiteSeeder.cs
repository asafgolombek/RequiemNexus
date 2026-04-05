using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Initial full import of sorcery rite definitions from the canonical catalog when the table is empty.
/// </summary>
public sealed class SorceryRiteSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 110;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.SorceryRiteDefinitions.AnyAsync())
        {
            return;
        }

        var covenants = await context.CovenantDefinitions.ToListAsync();
        var disciplines = await context.Disciplines.ToListAsync();

        var crone = covenants.FirstOrDefault(c => c.Name == "The Circle of the Crone");
        var lancea = covenants.FirstOrDefault(c => c.Name == "The Lancea et Sanctum");
        var cruacDisc = disciplines.FirstOrDefault(d => d.Name == "Crúac");
        var thebanDisc = disciplines.FirstOrDefault(d => d.Name == "Theban Sorcery");
        Discipline? necromancyDisc = disciplines.FirstOrDefault(d => d.Name == "Necromancy");

        if (crone == null || lancea == null || cruacDisc == null || thebanDisc == null)
        {
            return;
        }

        await SorceryRiteSeedingHelper.EnsureDisciplineExistsAsync(
            context,
            "Necromancy",
            "Kindred death sorcery — corpses, shades, and the other side.");
        necromancyDisc ??= await context.Disciplines.AsNoTracking().FirstOrDefaultAsync(d => d.Name == "Necromancy");

        IReadOnlyList<SorceryRiteCatalogEntry> catalog = SorceryRiteSeedData.LoadCatalogEntries(logger);
        var rites = new List<SorceryRiteDefinition>();

        foreach (SorceryRiteCatalogEntry entry in catalog)
        {
            switch (entry.SorceryType)
            {
                case SorceryType.Cruac:
                    rites.Add(SorceryRiteSeedingHelper.BuildSorceryRiteFromCatalogEntry(entry, crone.Id, requiredClanId: null, cruacDisc.Id));
                    break;
                case SorceryType.Theban:
                    rites.Add(SorceryRiteSeedingHelper.BuildSorceryRiteFromCatalogEntry(entry, lancea.Id, requiredClanId: null, thebanDisc.Id));
                    break;
                case SorceryType.Necromancy when necromancyDisc != null:
                    rites.Add(SorceryRiteSeedingHelper.BuildSorceryRiteFromCatalogEntry(entry, requiredCovenantId: null, requiredClanId: null, necromancyDisc.Id));
                    break;
            }
        }

        await context.SorceryRiteDefinitions.AddRangeAsync(rites);
        await context.SaveChangesAsync();
    }
}
