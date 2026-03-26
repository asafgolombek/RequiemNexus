using System.Text.Json;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Loads Scale and Coil definitions from SeedSource/coils_info.json for database seeding.
/// Each entry in coils_info.json is one Scale with 5 Coil tiers.
/// </summary>
public static class CoilSeedData
{
    /// <summary>
    /// Loads Scale and Coil definitions from SeedSource/coils_info.json.
    /// Returns a list of (Scale, Coils) tuples with prerequisite chain constructed in-memory.
    /// Scales and Coils do not yet have database Ids when returned — caller assigns them on insert.
    /// </summary>
    public static List<(ScaleDefinition Scale, List<CoilDefinition> Coils)> LoadFromDocs(ILogger logger)
    {
        using JsonDocument? doc = SeedDataLoader.TryLoadJson("coils_info.json", logger);
        if (doc == null)
        {
            return GetMinimalSeed();
        }

        return ParseCoils(doc.RootElement);
    }

    private static List<(ScaleDefinition Scale, List<CoilDefinition> Coils)> ParseCoils(JsonElement root)
    {
        var result = new List<(ScaleDefinition, List<CoilDefinition>)>();

        foreach (var scaleEl in root.EnumerateArray())
        {
            string scaleName = scaleEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
            string mysteryName = scaleEl.TryGetProperty("mystery", out var mystEl) ? mystEl.GetString() ?? string.Empty : string.Empty;
            string description = scaleEl.TryGetProperty("short Description", out var descEl) ? descEl.GetString() ?? string.Empty : string.Empty;

            if (string.IsNullOrEmpty(scaleName))
            {
                continue;
            }

            var scale = new ScaleDefinition
            {
                Name = scaleName,
                Description = description,
                MysteryName = mysteryName,
                MaxLevel = 5,
            };

            var coils = new List<CoilDefinition>();

            if (scaleEl.TryGetProperty("powers", out var powersEl))
            {
                CoilDefinition? previousCoil = null;

                foreach (var powerEl in powersEl.EnumerateArray())
                {
                    string coilName = powerEl.TryGetProperty("name", out var cnEl) ? cnEl.GetString() ?? string.Empty : string.Empty;
                    int ranking = powerEl.TryGetProperty("ranking", out var rankEl) ? rankEl.GetInt32() : 0;
                    string coilDesc = powerEl.TryGetProperty("description", out var cdEl) ? cdEl.GetString() ?? string.Empty : string.Empty;
                    string roll = powerEl.TryGetProperty("roll", out var rollEl) ? rollEl.GetString() ?? string.Empty : string.Empty;

                    if (string.IsNullOrEmpty(coilName) || ranking == 0)
                    {
                        continue;
                    }

                    var coil = new CoilDefinition
                    {
                        Name = coilName,
                        Description = coilDesc,
                        Level = ranking,
                        RollDescription = string.Equals(roll, "None", StringComparison.OrdinalIgnoreCase) ? null : roll,
                    };

                    // Prerequisite chain: each coil references the prior tier
                    // (IDs are not yet assigned here; DbInitializer resolves them after insertion)
                    if (previousCoil != null)
                    {
                        coil.PrerequisiteCoil = previousCoil;
                    }

                    coils.Add(coil);
                    previousCoil = coil;
                }
            }

            result.Add((scale, coils));
        }

        return result;
    }

    private static List<(ScaleDefinition Scale, List<CoilDefinition> Coils)> GetMinimalSeed()
    {
        var scale = new ScaleDefinition
        {
            Name = "Coil of the Ascendant",
            Description = "Focuses on conquering vulnerabilities to fire and sunlight.",
            MysteryName = "Mystery of the Ascendant",
            MaxLevel = 5,
        };

        CoilDefinition? prev = null;
        var coils = new List<CoilDefinition>();
        string[] names = ["Surmounting the Daysleep", "The Warm Face", "Conquer the Red Fear", "Peace with the Flame", "Sun's Forgotten Kiss"];
        for (int i = 0; i < names.Length; i++)
        {
            var coil = new CoilDefinition
            {
                Name = names[i],
                Description = $"Tier {i + 1} of the Coil of the Ascendant.",
                Level = i + 1,
                PrerequisiteCoil = prev,
            };
            coils.Add(coil);
            prev = coil;
        }

        return [(scale, coils)];
    }
}
