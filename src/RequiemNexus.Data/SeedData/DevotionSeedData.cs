using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Seed data for devotion definitions. Loads from SeedSource/devotions.json when available.
/// Phase 8: additive pools only.
/// </summary>
public static class DevotionSeedData
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Creates sample devotion definitions with prerequisites and pool definitions.
    /// Requires disciplines to be seeded first.
    /// </summary>
    public static List<DevotionDefinition> GetSampleDevotions(List<Discipline> disciplines)
    {
        Discipline Disc(string name) => disciplines.First(d => d.Name == name);

        var devotions = new List<DevotionDefinition>();

        // Body of Will: Stamina + Survival + Resilience
        devotions.Add(CreateDevotion(
            name: "Body of Will",
            description: "Ignore wound penalties, free Vigor effects.",
            xpCost: 2,
            activationCost: "●",
            isPassive: false,
            pool: new PoolDefinition(
            [
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Survival, null),
                new TraitReference(TraitType.Discipline, null, null, Disc("Resilience").Id),
            ]),
            prerequisites: [(Disc("Resilience").Id, 3), (Disc("Vigor").Id, 1)],
            orGroupId: 0,
            source: "VTR 2e 142"));

        // Best Served Cold: Stamina + Athletics + Vigor
        devotions.Add(CreateDevotion(
            name: "Best Served Cold",
            description: "Until the end of the chapter or the person who harmed you is defeated, +3 to attack them. Can only have one target at a time.",
            xpCost: 1,
            activationCost: "●●",
            isPassive: false,
            pool: new PoolDefinition(
            [
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Athletics, null),
                new TraitReference(TraitType.Discipline, null, null, Disc("Vigor").Id),
            ]),
            prerequisites: [(Disc("Vigor").Id, 3)],
            orGroupId: 0,
            source: "GTTN 135"));

        // Blood Scenting: Wits + Composure + Auspex
        devotions.Add(CreateDevotion(
            name: "Blood Scenting",
            description: "Identify the target's clan, blood potency and disciplines.",
            xpCost: 1,
            activationCost: "●",
            isPassive: false,
            pool: new PoolDefinition(
            [
                new TraitReference(TraitType.Attribute, AttributeId.Wits, null, null),
                new TraitReference(TraitType.Attribute, AttributeId.Composure, null, null),
                new TraitReference(TraitType.Discipline, null, null, Disc("Auspex").Id),
            ]),
            prerequisites: [(Disc("Auspex").Id, 3)],
            orGroupId: 0,
            source: "GTTN 136"));

        // Bones of the Mountain: Stamina + Survival + Protean
        devotions.Add(CreateDevotion(
            name: "Bones of the Mountain",
            description: "Become living stone for a turn, adding Protean to Resilience, dealing lethal unarmed, and activating Resilience and Vigor for free.",
            xpCost: 5,
            activationCost: "●●●",
            isPassive: false,
            pool: new PoolDefinition(
            [
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Survival, null),
                new TraitReference(TraitType.Discipline, null, null, Disc("Protean").Id),
            ]),
            prerequisites: [(Disc("Protean").Id, 4), (Disc("Resilience").Id, 3), (Disc("Vigor").Id, 3)],
            orGroupId: 0,
            source: "TY 73"));

        return devotions;
    }

    /// <summary>
    /// Loads devotion definitions from SeedSource/devotions.json when available.
    /// Parses prerequisites (e.g. "Resilience •••, Vigor •") and dice_pool (e.g. "Stamina + Survival + Resilience").
    /// Skips devotions with unknown disciplines. Falls back to <see cref="GetSampleDevotions"/> when file is missing.
    /// </summary>
    public static List<DevotionDefinition> LoadFromDocs(List<Discipline> disciplines, ILogger logger)
    {
        using JsonDocument? doc = SeedDataLoader.TryLoadJson("devotions.json", logger);
        if (doc == null)
        {
            return GetSampleDevotions(disciplines);
        }

        var result = new List<DevotionDefinition>();
        var disciplineByName = disciplines.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);

        foreach (var el in doc.RootElement.EnumerateArray())
        {
            DevotionDefinition? def = TryCreateDefinitionFromElement(el, disciplineByName, disciplines);
            if (def != null)
            {
                result.Add(def);
            }
        }

        if (result.Count < 4)
        {
            return GetSampleDevotions(disciplines);
        }

        return result;
    }

    /// <summary>
    /// Inserts devotion rows from <c>devotions.json</c> whose names are not already in the database.
    /// </summary>
    public static async Task EnsureMissingDefinitionsAsync(RequiemNexus.Data.ApplicationDbContext context, ILogger logger)
    {
        using JsonDocument? doc = SeedDataLoader.TryLoadJson("devotions.json", logger);
        if (doc == null)
        {
            return;
        }

        List<Discipline> disciplines = await context.Disciplines.ToListAsync();
        var disciplineByName = disciplines.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);
        HashSet<string> existing = await context.DevotionDefinitions
            .Select(d => d.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase);

        bool any = false;
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            DevotionDefinition? def = TryCreateDefinitionFromElement(el, disciplineByName, disciplines);
            if (def == null || existing.Contains(def.Name))
            {
                continue;
            }

            context.DevotionDefinitions.Add(def);
            existing.Add(def.Name);
            any = true;
        }

        if (any)
        {
            await context.SaveChangesAsync();
        }
    }

    private static DevotionDefinition? TryCreateDefinitionFromElement(
        JsonElement el,
        IReadOnlyDictionary<string, Discipline> disciplineByName,
        List<Discipline> disciplines)
    {
        string name = el.TryGetProperty("name", out var nEl) ? nEl.GetString() ?? string.Empty : string.Empty;
        string description = el.TryGetProperty("description", out var dEl) ? dEl.GetString() ?? string.Empty : string.Empty;
        string xpStr = el.TryGetProperty("xp", out var xEl) ? xEl.GetString() ?? "1" : "1";
        string cost = el.TryGetProperty("cost", out var cEl) ? cEl.GetString() ?? "—" : "—";
        string dicePool = el.TryGetProperty("dice_pool", out var dpEl) ? dpEl.GetString() ?? "None" : "None";
        string prereqStr = el.TryGetProperty("prerequisites", out var pEl) ? pEl.GetString() ?? string.Empty : string.Empty;
        string source = el.TryGetProperty("source", out var sEl) ? sEl.GetString() ?? string.Empty : string.Empty;

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (!int.TryParse(xpStr, out int xpCost) || xpCost < 0)
        {
            xpCost = 1;
        }

        var prereqs = ParsePrerequisites(prereqStr, disciplineByName);
        if (prereqs == null)
        {
            return null;
        }

        bool isPassive = string.Equals(dicePool, "None", StringComparison.OrdinalIgnoreCase);
        PoolDefinition? pool = null;
        if (!isPassive && !string.IsNullOrWhiteSpace(dicePool))
        {
            pool = TryParseDicePool(dicePool, disciplines);
        }

        var def = new DevotionDefinition
        {
            Name = name,
            Description = description,
            XpCost = xpCost,
            ActivationCostDescription = cost,
            IsPassive = isPassive,
            PoolDefinitionJson = pool != null ? JsonSerializer.Serialize(pool, _jsonOptions) : null,
            Source = source,
        };

        foreach (var (disciplineId, minLevel, orGroupId) in prereqs)
        {
            def.Prerequisites.Add(new DevotionPrerequisite
            {
                DisciplineId = disciplineId,
                MinimumLevel = minLevel,
                OrGroupId = orGroupId,
            });
        }

        return def;
    }

    private static List<(int DisciplineId, int MinLevel, int OrGroupId)>? ParsePrerequisites(
        string prereqStr,
        IReadOnlyDictionary<string, Discipline> disciplineByName)
    {
        if (string.IsNullOrWhiteSpace(prereqStr))
        {
            return [];
        }

        var result = new List<(int, int, int)>();
        int orGroupId = 0;

        foreach (var part in prereqStr.Split(',', StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            if (part.Contains(" or ", StringComparison.OrdinalIgnoreCase))
            {
                var orParts = part.Split(" or ", StringSplitOptions.TrimEntries);
                int level = 0;
                foreach (var op in orParts)
                {
                    var (discName, lvl) = ParseDisciplineLevel(op.Trim());
                    if (lvl > 0)
                    {
                        level = lvl;
                    }
                }

                level = level > 0 ? level : 1;
                orGroupId++;

                foreach (var op in orParts)
                {
                    var (discName, lvl) = ParseDisciplineLevel(op.Trim());
                    int actualLevel = lvl > 0 ? lvl : level;
                    if (string.IsNullOrEmpty(discName) || !disciplineByName.TryGetValue(discName, out var disc))
                    {
                        continue;
                    }

                    result.Add((disc.Id, actualLevel, orGroupId));
                }
            }
            else
            {
                var (discName, level) = ParseDisciplineLevel(part);
                if (string.IsNullOrEmpty(discName) || !disciplineByName.TryGetValue(discName, out var disc))
                {
                    return null;
                }

                result.Add((disc.Id, level > 0 ? level : 1, 0));
            }
        }

        return result.Count > 0 ? result : null;
    }

    /// <summary>
    /// Normalizes bullet/dot characters for level parsing.
    /// Handles Unicode bullet (•), asterisk, and common mojibake from encoding issues.
    /// </summary>
    private static string NormalizeBullets(string s)
    {
        s = s.Replace('\u2022', '*'); // •
        s = s.Replace("\u0393\u00C7\u00F3", "*"); // ΓÇó mojibake
        s = s.Replace("\u00E2\u20AC\u00A2", "*"); // â€¢ mojibake
        return s;
    }

    private static (string DisciplineName, int Level) ParseDisciplineLevel(string s)
    {
        s = NormalizeBullets(s.Trim());
        var parenIdx = s.IndexOf('(');
        if (parenIdx >= 0)
        {
            s = s[..parenIdx].Trim();
        }

        int dotCount = 0;
        int i = s.Length - 1;
        while (i >= 0 && (s[i] == '•' || s[i] == '*' || char.IsWhiteSpace(s[i])))
        {
            if (s[i] == '•' || s[i] == '*')
            {
                dotCount++;
            }

            i--;
        }

        string name = (i >= 0 ? s[..(i + 1)] : s).Trim();
        return (name, dotCount);
    }

    private static PoolDefinition? TryParseDicePool(string dicePool, List<Discipline> disciplines)
    {
        var primary = dicePool.Split(" vs ", StringSplitOptions.TrimEntries)[0].Trim();
        var tokens = primary.Split('+', StringSplitOptions.TrimEntries);
        var traits = new List<TraitReference>();
        var disciplineByName = disciplines.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            var t = token.Trim();
            if (string.IsNullOrEmpty(t))
            {
                continue;
            }

            if (Enum.TryParse<AttributeId>(t.Replace(" ", string.Empty), true, out var attrId))
            {
                traits.Add(new TraitReference(TraitType.Attribute, attrId, null, null));
                continue;
            }

            if (TryParseSkillId(t, out var skillId))
            {
                traits.Add(new TraitReference(TraitType.Skill, null, skillId, null));
                continue;
            }

            if (disciplineByName.TryGetValue(t, out var disc))
            {
                traits.Add(new TraitReference(TraitType.Discipline, null, null, disc.Id));
            }
        }

        return traits.Count > 0 ? new PoolDefinition(traits) : null;
    }

    private static bool TryParseSkillId(string name, out SkillId skillId)
    {
        var normalized = name.Replace(" ", string.Empty);
        return Enum.TryParse(normalized, true, out skillId);
    }

    private static DevotionDefinition CreateDevotion(
        string name,
        string description,
        int xpCost,
        string activationCost,
        bool isPassive,
        PoolDefinition pool,
        List<(int DisciplineId, int MinLevel)> prerequisites,
        int orGroupId,
        string source)
    {
        var def = new DevotionDefinition
        {
            Name = name,
            Description = description,
            XpCost = xpCost,
            ActivationCostDescription = activationCost,
            IsPassive = isPassive,
            PoolDefinitionJson = JsonSerializer.Serialize(pool, _jsonOptions),
            Source = source,
        };

        foreach (var (disciplineId, minLevel) in prerequisites)
        {
            def.Prerequisites.Add(new DevotionPrerequisite
            {
                DisciplineId = disciplineId,
                MinimumLevel = minLevel,
                OrGroupId = orGroupId,
            });
        }

        return def;
    }
}
