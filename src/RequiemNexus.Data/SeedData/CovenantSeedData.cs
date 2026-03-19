using System.Text.Json;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Seed data for covenant definitions. Loads from SeedSource/Covenants.json when available.
/// VII is seeded for worldbuilding but IsPlayable = false (block character join).
/// </summary>
public static class CovenantSeedData
{
    /// <summary>
    /// Loads covenant definitions from SeedSource/Covenants.json when available.
    /// Maps short description to Description; VII is not playable; Crone and Lancea support Blood Sorcery.
    /// Falls back to <see cref="GetAllCovenants"/> when file is missing or invalid.
    /// </summary>
    public static List<CovenantDefinition> LoadFromDocs()
    {
        string? seedDir = SeedSourcePathResolver.GetSeedDirectory();
        if (seedDir == null)
        {
            return GetAllCovenants();
        }

        var path = Path.Combine(seedDir, "Covenants.json");
        if (!File.Exists(path))
        {
            return GetAllCovenants();
        }

        try
        {
            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var result = new List<CovenantDefinition>();

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                string name = el.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? string.Empty : string.Empty;
                string description = el.TryGetProperty("short description", out var descEl) ? descEl.GetString() ?? string.Empty : string.Empty;

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                bool isPlayable = !string.Equals(name, "VII", StringComparison.OrdinalIgnoreCase);
                bool supportsBloodSorcery = string.Equals(name, "The Circle of the Crone", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "The Lancea et Sanctum", StringComparison.OrdinalIgnoreCase);

                result.Add(new CovenantDefinition
                {
                    Name = name,
                    Description = description,
                    IsPlayable = isPlayable,
                    SupportsBloodSorcery = supportsBloodSorcery,
                });
            }

            return result.Count > 0 ? result : GetAllCovenants();
        }
        catch
        {
            return GetAllCovenants();
        }
    }

    /// <summary>
    /// Creates covenant definitions inline. Five core covenants are playable; VII is not.
    /// Used when LoadFromDocs cannot read the JSON file.
    /// </summary>
    public static List<CovenantDefinition> GetAllCovenants()
    {
        return
        [
            new CovenantDefinition
            {
                Name = "The Carthian Movement",
                Description = "Vampiric idealists applying modern mortal political systems and democracy to Kindred society.",
                IsPlayable = true,
            },
            new CovenantDefinition
            {
                Name = "The Circle of the Crone",
                Description = "A covenant of ritualistic Kindred who revere pagan gods, spirits, pantheons, and progenitors.",
                IsPlayable = true,
                SupportsBloodSorcery = true,
            },
            new CovenantDefinition
            {
                Name = "The Invictus",
                Description = "A covenant of vampires determined to protect the Masquerade and rule as elites.",
                IsPlayable = true,
            },
            new CovenantDefinition
            {
                Name = "The Lancea et Sanctum",
                Description = "The vampiric church believing Kindred are cursed to do God's dark work.",
                IsPlayable = true,
                SupportsBloodSorcery = true,
            },
            new CovenantDefinition
            {
                Name = "The Ordo Dracul",
                Description = "A covenant of vampires known for mystic studies and desire to transcend the curse.",
                IsPlayable = true,
            },
            new CovenantDefinition
            {
                Name = "VII",
                Description = "A mysterious group of vampires that detests Kindred and seeks to destroy them.",
                IsPlayable = false,
            },
        ];
    }
}
