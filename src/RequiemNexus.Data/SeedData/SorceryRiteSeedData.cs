using System.Text.Json;
using Microsoft.Extensions.Logging;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Loads Crúac rites, Theban rituals, and Kindred Necromancy rites from SeedSource JSON for database seeding.
/// Falls back to a minimal inline set when primary files are not found.
/// </summary>
public static class SorceryRiteSeedData
{
    /// <summary>
    /// Loads the full sorcery catalog for idempotent upserts (name-keyed).
    /// </summary>
    public static List<SorceryRiteCatalogEntry> LoadCatalogEntries(ILogger logger)
    {
        var result = new List<SorceryRiteCatalogEntry>();

        using (JsonDocument? ritesDoc = SeedDataLoader.TryLoadJson("rites.json", logger))
        {
            if (ritesDoc != null)
            {
                AppendArray(ritesDoc.RootElement, SorceryType.Cruac, result);
            }
            else
            {
                foreach (var e in GetMinimalCruacEntries())
                {
                    result.Add(e);
                }
            }
        }

        using (JsonDocument? ritualsDoc = SeedDataLoader.TryLoadJson("rituals.json", logger))
        {
            if (ritualsDoc != null)
            {
                AppendArray(ritualsDoc.RootElement, SorceryType.Theban, result);
            }
            else
            {
                foreach (var e in GetMinimalThebanEntries())
                {
                    result.Add(e);
                }
            }
        }

        using (JsonDocument? necroDoc = SeedDataLoader.TryLoadJson("necromancyRites.json", logger))
        {
            if (necroDoc != null)
            {
                AppendArray(necroDoc.RootElement, SorceryType.Necromancy, result);
            }
        }

        if (result.Count == 0)
        {
            foreach (var e in GetMinimalCruacEntries())
            {
                result.Add(e);
            }

            foreach (var e in GetMinimalThebanEntries())
            {
                result.Add(e);
            }
        }

        return result;
    }

    /// <summary>
    /// Loads rites from SeedSource JSON. Legacy tuple shape for <see cref="DbInitializer.SeedSorceryRitesAsync"/>.
    /// </summary>
    public static List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType)> LoadFromDocs(ILogger logger)
    {
        return LoadCatalogEntries(logger)
            .Select(e => (e.Name, e.Rating, e.Prerequisites, e.Effect, e.SorceryType))
            .ToList();
    }

    private static void AppendArray(JsonElement root, SorceryType sorceryType, List<SorceryRiteCatalogEntry> target)
    {
        foreach (var el in root.EnumerateArray())
        {
            string name = el.GetProperty("name").GetString() ?? string.Empty;
            int rating = el.TryGetProperty("Rating", out var r) ? r.GetInt32() : 1;
            string prereq = el.TryGetProperty("Prerequisites", out var p) ? p.GetString() ?? string.Empty : string.Empty;
            string effect = el.TryGetProperty("Effect", out var e) ? e.GetString() ?? string.Empty : string.Empty;
            if (!string.IsNullOrEmpty(name))
            {
                target.Add(new SorceryRiteCatalogEntry(name, rating, prereq, effect, sorceryType));
            }
        }
    }

    private static IEnumerable<SorceryRiteCatalogEntry> GetMinimalCruacEntries()
    {
        yield return new SorceryRiteCatalogEntry(
            "Lair of the Beast",
            1,
            "Smear Vitae over a central point of the territory.",
            "Extends the vampire's Predatory Aura over the entire territory.",
            SorceryType.Cruac);
        yield return new SorceryRiteCatalogEntry(
            "Pangs of Proserpina",
            2,
            "Target must be within a mile.",
            "Inflicts intense hunger on a victim, provoking frenzy in vampires.",
            SorceryType.Cruac);
        yield return new SorceryRiteCatalogEntry(
            "The Hydra's Vitae",
            3,
            "None specified.",
            "Transforms the ritualist's own blood into a toxic poison.",
            SorceryType.Cruac);
    }

    private static IEnumerable<SorceryRiteCatalogEntry> GetMinimalThebanEntries()
    {
        yield return new SorceryRiteCatalogEntry(
            "Apple of Eden",
            1,
            "Sacrament: An apple, a drop of Vitae.",
            "Grants temporary Intelligence and Wits dots.",
            SorceryType.Theban);
        yield return new SorceryRiteCatalogEntry(
            "Blood Scourge",
            1,
            "Sacrament: The ritualist's own blood (at least one Vitae).",
            "Transforms blood into a stinging whip.",
            SorceryType.Theban);
        yield return new SorceryRiteCatalogEntry(
            "Marian Apparition",
            2,
            "Sacrament: A piece of pure white cloth.",
            "Creates an apparition of a holy figure.",
            SorceryType.Theban);
    }
}
