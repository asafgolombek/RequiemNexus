using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Ensures Phase 9.5/9.6 disciplines, covenant flags, default requirements JSON, and sorcery catalog upserts stay aligned with seed data.
/// </summary>
public sealed class BloodSorceryExtensionSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 120;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        await SorceryRiteSeedingHelper.EnsureDisciplineExistsAsync(
            context,
            "Necromancy",
            "Kindred death sorcery — corpses, shades, and the other side.");

        List<SorceryRiteDefinition> missingReq = await context.SorceryRiteDefinitions
            .Where(r => r.RequirementsJson == null || r.RequirementsJson == string.Empty)
            .ToListAsync();
        foreach (SorceryRiteDefinition row in missingReq)
        {
            row.RequirementsJson = SorceryRiteSeedingHelper.DefaultRiteRequirementsJson;
        }

        if (missingReq.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        await context.SaveChangesAsync();
        await EnsureMissingSorceryRiteCatalogEntriesAsync(context, logger);
        await EnsureSorceryRiteCostsCorrectAsync(context);
        await ClearNecromancyRequiredClanGateAsync(context);
        await EnsureSorceryRiteCatalogAlignmentFromSeedAsync(context, logger);
    }

    /// <summary>
    /// Kindred Necromancy is not clan-restricted in V:tR 2e; clears legacy Mekhet-only gates from seed rows.
    /// </summary>
    private static async Task ClearNecromancyRequiredClanGateAsync(ApplicationDbContext context)
    {
        List<SorceryRiteDefinition> gated = await context.SorceryRiteDefinitions
            .Where(r => r.SorceryType == SorceryType.Necromancy && r.RequiredClanId != null)
            .ToListAsync();

        if (gated.Count == 0)
        {
            return;
        }

        foreach (SorceryRiteDefinition r in gated)
        {
            r.RequiredClanId = null;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Patches RequirementsJson and ActivationCostDescription for already-seeded rites to match correct rule costs.
    /// Crúac costs Level Vitae; Theban costs 1 Willpower + Sacrament; Necromancy costs 1 Vitae + focus.
    /// </summary>
    private static async Task EnsureSorceryRiteCostsCorrectAsync(ApplicationDbContext context)
    {
        List<SorceryRiteDefinition> cruacRites = await context.SorceryRiteDefinitions
            .Where(r => r.SorceryType == SorceryType.Cruac)
            .ToListAsync();
        foreach (SorceryRiteDefinition r in cruacRites)
        {
            r.RequirementsJson = $$"""[{"type":"InternalVitae","value":{{r.Level}},"isConsumed":true}]""";
            r.ActivationCostDescription = $"{r.Level} Vitae";
        }

        List<SorceryRiteDefinition> thebanRites = await context.SorceryRiteDefinitions
            .Where(r => r.SorceryType == SorceryType.Theban)
            .ToListAsync();
        foreach (SorceryRiteDefinition r in thebanRites)
        {
            r.RequirementsJson = SorceryRiteSeedingHelper.BuildThebanRequirementsJson(r.Prerequisites);
            r.ActivationCostDescription = "1 Willpower + Sacrament";
        }

        List<SorceryRiteDefinition> necroRites = await context.SorceryRiteDefinitions
            .Where(r => r.SorceryType == SorceryType.Necromancy
                   && r.RequirementsJson == SorceryRiteSeedingHelper.DefaultRiteRequirementsJson)
            .ToListAsync();
        foreach (SorceryRiteDefinition r in necroRites)
        {
            r.RequirementsJson = """[{"type":"MaterialFocus","value":1,"isConsumed":false},{"type":"InternalVitae","value":1,"isConsumed":true}]""";
            r.ActivationCostDescription = "1 Vitae + focus";
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Inserts sorcery definitions from seed JSON for names not already present (Crúac, Theban, Necromancy).
    /// </summary>
    private static async Task EnsureMissingSorceryRiteCatalogEntriesAsync(ApplicationDbContext context, ILogger logger)
    {
        HashSet<string> existing = await context.SorceryRiteDefinitions
            .Select(r => r.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

        List<CovenantDefinition> covenants = await context.CovenantDefinitions.ToListAsync();
        List<Discipline> disciplines = await context.Disciplines.ToListAsync();

        CovenantDefinition? crone = covenants.FirstOrDefault(c => c.Name == "The Circle of the Crone");
        CovenantDefinition? lancea = covenants.FirstOrDefault(c => c.Name == "The Lancea et Sanctum");
        Discipline? cruacDisc = disciplines.FirstOrDefault(d => d.Name == "Crúac");
        Discipline? thebanDisc = disciplines.FirstOrDefault(d => d.Name == "Theban Sorcery");
        Discipline? necromancy = disciplines.FirstOrDefault(d => d.Name == "Necromancy");

        if (crone == null || lancea == null || cruacDisc == null || thebanDisc == null)
        {
            logger.LogWarning(
                "Skipping sorcery catalog upsert: missing Crúac/Theban covenant or discipline gates.");
            return;
        }

        IReadOnlyList<SorceryRiteCatalogEntry> catalog = SorceryRiteSeedData.LoadCatalogEntries(logger);
        var toAdd = new List<SorceryRiteDefinition>();

        foreach (SorceryRiteCatalogEntry entry in catalog)
        {
            if (existing.Contains(entry.Name))
            {
                continue;
            }

            switch (entry.SorceryType)
            {
                case SorceryType.Cruac:
                    toAdd.Add(SorceryRiteSeedingHelper.BuildSorceryRiteFromCatalogEntry(entry, crone.Id, requiredClanId: null, cruacDisc.Id));
                    break;
                case SorceryType.Theban:
                    toAdd.Add(SorceryRiteSeedingHelper.BuildSorceryRiteFromCatalogEntry(entry, lancea.Id, requiredClanId: null, thebanDisc.Id));
                    break;
                case SorceryType.Necromancy:
                    if (necromancy == null)
                    {
                        continue;
                    }

                    toAdd.Add(SorceryRiteSeedingHelper.BuildSorceryRiteFromCatalogEntry(entry, requiredCovenantId: null, requiredClanId: null, necromancy.Id));
                    break;
            }

            existing.Add(entry.Name);
        }

        if (toAdd.Count > 0)
        {
            await context.SorceryRiteDefinitions.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Aligns level, XP cost, target successes, and elder flag with the canonical seed catalog (name + tradition match).
    /// </summary>
    private static async Task EnsureSorceryRiteCatalogAlignmentFromSeedAsync(ApplicationDbContext context, ILogger logger)
    {
        IReadOnlyList<SorceryRiteCatalogEntry> catalog = SorceryRiteSeedData.LoadCatalogEntries(logger);
        var byKey = catalog.ToDictionary(
            e => (e.Name, e.SorceryType),
            e => e,
            comparer: new SorceryCatalogKeyComparer());

        List<SorceryRiteDefinition> rows = await context.SorceryRiteDefinitions.ToListAsync();
        int changed = 0;
        foreach (SorceryRiteDefinition row in rows)
        {
            if (!byKey.TryGetValue((row.Name, row.SorceryType), out SorceryRiteCatalogEntry? entry))
            {
                if (row.TargetSuccesses < 1)
                {
                    row.TargetSuccesses = SorceryRiteSeedData.DefaultTargetSuccessesForRating(row.Level);
                    changed++;
                }

                continue;
            }

            if (row.TargetSuccesses != entry.TargetSuccesses)
            {
                row.TargetSuccesses = entry.TargetSuccesses;
                changed++;
            }

            if (row.Level != entry.Rating || row.XpCost != entry.Rating)
            {
                row.Level = entry.Rating;
                row.XpCost = entry.Rating;
                changed++;
            }

            if (row.RequiresElder != entry.RequiresElder)
            {
                row.RequiresElder = entry.RequiresElder;
                changed++;
            }
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Updated {Changed} sorcery rite field(s) from canonical seed catalog.",
                changed);
        }
    }
}
