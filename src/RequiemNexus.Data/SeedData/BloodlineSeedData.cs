using System.Text.Json;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Seed data for bloodline definitions. Loads from SeedSource/bloodlines.json when available.
/// FourthDisciplineId is derived: the discipline in the bloodline's 4 that is not in the parent clan's 3.
/// </summary>
public static class BloodlineSeedData
{
    private static readonly IReadOnlyDictionary<string, string[]> _clanDisciplineNames = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Daeva"] = ["Celerity", "Majesty", "Vigor"],
        ["Gangrel"] = ["Animalism", "Protean", "Resilience"],
        ["Mekhet"] = ["Auspex", "Celerity", "Obfuscate"],
        ["Nosferatu"] = ["Nightmare", "Obfuscate", "Vigor"],
        ["Ventrue"] = ["Animalism", "Dominate", "Resilience"],
    };

    /// <summary>
    /// Loads bloodline definitions from SeedSource/bloodlines.json when available.
    /// Skips bloodlines that reference disciplines not in the database.
    /// Falls back to <see cref="GetAllBloodlines"/> when file is missing or invalid.
    /// </summary>
    public static List<BloodlineDefinition> LoadFromDocs(List<Clan> clans, List<Discipline> disciplines)
    {
        string? seedDir = SeedSourcePathResolver.GetSeedDirectory();
        if (seedDir == null)
        {
            return GetAllBloodlines(clans, disciplines);
        }

        var path = Path.Combine(seedDir, "bloodlines.json");
        if (!File.Exists(path))
        {
            return GetAllBloodlines(clans, disciplines);
        }

        try
        {
            string json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var result = new List<BloodlineDefinition>();
            var clanByName = clans.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
            var disciplineByName = disciplines.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var parentEl in doc.RootElement.EnumerateArray())
            {
                string parentClanName = parentEl.TryGetProperty("parent_clan", out var pcEl) ? pcEl.GetString() ?? string.Empty : string.Empty;
                if (!clanByName.TryGetValue(parentClanName, out var clan) || !_clanDisciplineNames.TryGetValue(parentClanName, out var clanDiscNames))
                {
                    continue;
                }

                var clanDiscSet = new HashSet<string>(clanDiscNames, StringComparer.OrdinalIgnoreCase);

                if (!parentEl.TryGetProperty("bloodlines", out var bloodlinesEl))
                {
                    continue;
                }

                foreach (var blEl in bloodlinesEl.EnumerateArray())
                {
                    string name = blEl.TryGetProperty("name", out var nEl) ? nEl.GetString() ?? string.Empty : string.Empty;
                    string description = blEl.TryGetProperty("description", out var dEl) ? dEl.GetString() ?? string.Empty : string.Empty;
                    string weakness = blEl.TryGetProperty("weakness", out var wEl) ? wEl.GetString() ?? string.Empty : string.Empty;
                    string specialFeature = blEl.TryGetProperty("special_feature", out var sfEl) ? sfEl.GetString() ?? string.Empty : string.Empty;

                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    if (seenNames.Contains(name))
                    {
                        var existing = result.First(b => string.Equals(b.Name, name, StringComparison.OrdinalIgnoreCase));
                        existing.AllowedParentClans.Add(new BloodlineClan { ClanId = clan.Id });
                        continue;
                    }

                    if (!blEl.TryGetProperty("disciplines", out var discEl))
                    {
                        continue;
                    }

                    var blDiscNames = new List<string>();
                    foreach (var d in discEl.EnumerateArray())
                    {
                        string? dn = d.GetString();
                        if (!string.IsNullOrEmpty(dn))
                        {
                            blDiscNames.Add(dn);
                        }
                    }

                    if (blDiscNames.Count != 4)
                    {
                        continue;
                    }

                    string? fourthName = blDiscNames.FirstOrDefault(d => !clanDiscSet.Contains(d));
                    if (string.IsNullOrEmpty(fourthName) || !disciplineByName.TryGetValue(fourthName, out var fourthDisc))
                    {
                        continue;
                    }

                    var def = new BloodlineDefinition
                    {
                        Name = name,
                        Description = description,
                        FourthDisciplineId = fourthDisc.Id,
                        PrerequisiteBloodPotency = 2,
                        BaneOverride = weakness,
                        CustomRuleOverride = !string.Equals(specialFeature, "None", StringComparison.OrdinalIgnoreCase),
                        CustomRuleOverrideDescription = string.Equals(specialFeature, "None", StringComparison.OrdinalIgnoreCase) ? null : specialFeature,
                        AllowedParentClans = [new BloodlineClan { ClanId = clan.Id }],
                    };

                    result.Add(def);
                    seenNames.Add(name);
                }
            }

            return result.Count > 0 ? result : GetAllBloodlines(clans, disciplines);
        }
        catch
        {
            return GetAllBloodlines(clans, disciplines);
        }
    }

    /// <summary>
    /// Creates bloodline definitions with their allowed parent clans inline.
    /// Requires clans and disciplines to be seeded first (have IDs).
    /// Used when LoadFromDocs cannot read the JSON file.
    /// </summary>
    public static List<BloodlineDefinition> GetAllBloodlines(
        List<Clan> clans,
        List<Discipline> disciplines)
    {
        Clan Clan(string name) => clans.First(c => c.Name == name);
        Discipline Disc(string name) => disciplines.First(d => d.Name == name);

        var bloodlines = new List<BloodlineDefinition>();

        // Ankou (Mekhet): 4th = Vigor (Mekhet has Auspex, Celerity, Obfuscate)
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Ankou",
            Description = "Killers who keep the living separate from the dead for the greater good.",
            FourthDisciplineId = Disc("Vigor").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Must roll Humanity to recover Willpower from Mask",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Mekhet").Id },
            ],
        });

        // Icelus (Mekhet, Ventrue): 4th = Dominate for Mekhet; Auspex for Ventrue — use Dominate as canonical
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Icelus",
            Description = "Manipulators disguised as hypnotherapists plumbing the collective unconscious.",
            FourthDisciplineId = Disc("Dominate").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Detachment inflicts a Mesmerized state",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Fourth discipline varies by parent clan per VtR 2e.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Mekhet").Id },
                new BloodlineClan { ClanId = Clan("Ventrue").Id },
            ],
        });

        // Khaibit (Mekhet): 4th = Vigor
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Khaibit",
            Description = "An ancient line of soldiers who fight darkness by embracing it.",
            FourthDisciplineId = Disc("Vigor").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Temporarily blinded by any light bright enough to inhibit normal vision",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Mekhet").Id },
            ],
        });

        // Kerberos (Gangrel): 4th = Majesty
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Kerberos",
            Description = "Self-reinventing social predators who excel at projecting the Beast.",
            FourthDisciplineId = Disc("Majesty").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Lose 10-again to oppose characters without the Predatory Aura",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Hounds of Hell special feature.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Gangrel").Id },
            ],
        });

        // Lidérc (Daeva): 4th = Obfuscate
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Lidérc",
            Description = "Passionate lovers who drink from the ardor they incite.",
            FourthDisciplineId = Disc("Obfuscate").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "The Wanton Curse also disrupts Touchstones",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Siphon Devotions special feature.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Daeva").Id },
            ],
        });

        // Nosoi (Gangrel): 4th = Dominate
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Nosoi",
            Description = "Plague farmers who cultivate blood-borne disease in the herd.",
            FourthDisciplineId = Disc("Dominate").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Imbibed Vitae untainted by your disease is capped nightly by Humanity",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Bloodline Devotions special feature.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Gangrel").Id },
            ],
        });

        // Vardyvle (Ventrue): 4th = Protean
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Vardyvle",
            Description = "Jealous dreamers who yearn for what they are not and lose themselves in others' lives.",
            FourthDisciplineId = Disc("Protean").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Feeding from those living your dreams risks False Memories",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Shapeshifting Devotion special feature.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Ventrue").Id },
            ],
        });

        // Vilseduire (Mekhet, Nosferatu): 4th = Majesty (works for both)
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Vilseduire",
            Description = "Narcissistic rebels who revel in glamorous transgression.",
            FourthDisciplineId = Disc("Majesty").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Only one Touchstone, and risk detachment at lower Humanity",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Mekhet").Id },
                new BloodlineClan { ClanId = Clan("Nosferatu").Id },
            ],
        });

        return bloodlines;
    }
}
