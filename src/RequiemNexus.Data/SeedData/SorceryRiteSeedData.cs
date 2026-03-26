using System.Text.Json;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Loads Crúac rites and Theban rituals from SeedSource JSON files for database seeding.
/// Falls back to a minimal inline set if files are not found.
/// </summary>
public static class SorceryRiteSeedData
{
    /// <summary>
    /// Loads rites from SeedSource/rites.json (Crúac) and SeedSource/rituals.json (Theban).
    /// Returns list of (Name, Rating, Prerequisites, Effect, SorceryType).
    /// </summary>
    public static List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType)> LoadFromDocs(ILogger logger)
    {
        var result = new List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType)>();

        using (JsonDocument? ritesDoc = SeedDataLoader.TryLoadJson("rites.json", logger))
        {
            if (ritesDoc != null)
            {
                foreach (var el in ritesDoc.RootElement.EnumerateArray())
                {
                    string name = el.GetProperty("name").GetString() ?? string.Empty;
                    int rating = el.TryGetProperty("Rating", out var r) ? r.GetInt32() : 1;
                    string prereq = el.TryGetProperty("Prerequisites", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                    string effect = el.TryGetProperty("Effect", out var e) ? e.GetString() ?? string.Empty : string.Empty;
                    if (!string.IsNullOrEmpty(name))
                    {
                        result.Add((name, rating, prereq, effect, SorceryType.Cruac));
                    }
                }
            }
            else
            {
                result.AddRange(GetMinimalCruac());
            }
        }

        using (JsonDocument? ritualsDoc = SeedDataLoader.TryLoadJson("rituals.json", logger))
        {
            if (ritualsDoc != null)
            {
                foreach (var el in ritualsDoc.RootElement.EnumerateArray())
                {
                    string name = el.GetProperty("name").GetString() ?? string.Empty;
                    int rating = el.TryGetProperty("Rating", out var r) ? r.GetInt32() : 1;
                    string prereq = el.TryGetProperty("Prerequisites", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                    string effect = el.TryGetProperty("Effect", out var e) ? e.GetString() ?? string.Empty : string.Empty;
                    if (!string.IsNullOrEmpty(name))
                    {
                        result.Add((name, rating, prereq, effect, SorceryType.Theban));
                    }
                }
            }
            else
            {
                result.AddRange(GetMinimalTheban());
            }
        }

        return result.Count > 0 ? result : GetMinimalSeed();
    }

    private static List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType)> GetMinimalSeed()
    {
        var list = new List<(string, int, string, string, SorceryType)>();
        list.AddRange(GetMinimalCruac());
        list.AddRange(GetMinimalTheban());
        return list;
    }

    private static List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType)> GetMinimalCruac()
    {
        return
        [
            ("Lair of the Beast", 1, "Smear Vitae over a central point of the territory.", "Extends the vampire's Predatory Aura over the entire territory.", SorceryType.Cruac),
            ("Pangs of Proserpina", 2, "Target must be within a mile.", "Inflicts intense hunger on a victim, provoking frenzy in vampires.", SorceryType.Cruac),
            ("The Hydra's Vitae", 3, "None specified.", "Transforms the ritualist's own blood into a toxic poison.", SorceryType.Cruac),
        ];
    }

    private static List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType)> GetMinimalTheban()
    {
        return
        [
            ("Apple of Eden", 1, "Sacrament: An apple, a drop of Vitae.", "Grants temporary Intelligence and Wits dots.", SorceryType.Theban),
            ("Blood Scourge", 1, "Sacrament: The ritualist's own blood (at least one Vitae).", "Transforms blood into a stinging whip.", SorceryType.Theban),
            ("Marian Apparition", 2, "Sacrament: A piece of pure white cloth.", "Creates an apparition of a holy figure.", SorceryType.Theban),
        ];
    }
}
