using System.ComponentModel;
using System.Reflection;

namespace RequiemNexus.Domain;

/// <summary>
/// Maps V:tR 2e skill labels (e.g. from equipment JSON) to <see cref="SkillId"/>.
/// </summary>
public static class SkillBookNameParser
{
    private static readonly Dictionary<string, SkillId> _byDescription = BuildDescriptionMap();

    /// <summary>
    /// Attempts to parse a book-style skill name (table header or JSON) to <see cref="SkillId"/>.
    /// </summary>
    /// <param name="name">The skill label; comparison is case-insensitive and trims whitespace.</param>
    /// <param name="skillId">The resolved enum value when this method returns true.</param>
    /// <returns>True when a known skill was matched.</returns>
    public static bool TryParseBookName(string? name, out SkillId skillId)
    {
        skillId = default;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        string key = name.Trim();
        if (_byDescription.TryGetValue(key, out skillId))
        {
            return true;
        }

        // Enum member names without spaces (e.g. AnimalKen)
        string compact = key.Replace(" ", string.Empty, StringComparison.Ordinal);
        return Enum.TryParse(compact, ignoreCase: true, out skillId);
    }

    private static Dictionary<string, SkillId> BuildDescriptionMap()
    {
        var map = new Dictionary<string, SkillId>(StringComparer.OrdinalIgnoreCase);
        foreach (SkillId id in Enum.GetValues<SkillId>())
        {
            string? d = id.GetType().GetField(id.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrEmpty(d))
            {
                map[d] = id;
            }

            map[id.ToString()] = id;
        }

        return map;
    }
}
