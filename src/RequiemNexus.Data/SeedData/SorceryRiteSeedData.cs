using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Loads Crúac rites and Theban rituals from docs/ JSON files for database seeding.
/// Falls back to a minimal inline set if files are not found.
/// </summary>
public static class SorceryRiteSeedData
{
    /// <summary>
    /// Loads rites from docs/rites.json (Crúac) and docs/rituals.json (Theban).
    /// Returns list of (Name, Rating, Prerequisites, Effect, SorceryType).
    /// </summary>
    public static List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType)> LoadFromDocs()
    {
        var result = new List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType)>();

        var basePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "docs"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "docs"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "docs"),
        };

        string? docsDir = basePaths.FirstOrDefault(Directory.Exists);
        if (docsDir == null)
        {
            return GetMinimalSeed();
        }

        var ritesPath = Path.Combine(docsDir, "rites.json");
        if (File.Exists(ritesPath))
        {
            try
            {
                string json = File.ReadAllText(ritesPath);
                using var doc = JsonDocument.Parse(json);
                foreach (var el in doc.RootElement.EnumerateArray())
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
            catch
            {
                result.AddRange(GetMinimalCruac());
            }
        }
        else
        {
            result.AddRange(GetMinimalCruac());
        }

        var ritualsPath = Path.Combine(docsDir, "rituals.json");
        if (File.Exists(ritualsPath))
        {
            try
            {
                string json = File.ReadAllText(ritualsPath);
                using var doc = JsonDocument.Parse(json);
                foreach (var el in doc.RootElement.EnumerateArray())
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
            catch
            {
                result.AddRange(GetMinimalTheban());
            }
        }
        else
        {
            result.AddRange(GetMinimalTheban());
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
