using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Helpers;

/// <summary>
/// User-facing labels for discipline affinity (clan triple, bloodline fourth, XP tier, special gates).
/// </summary>
public static class DisciplineUiLabels
{
    /// <summary>
    /// Short badge text for a discipline row on the sheet or in lists (no discipline name).
    /// </summary>
    public static string FormatAffinityBadge(Character character, Discipline? discipline)
    {
        if (discipline == null)
        {
            return string.Empty;
        }

        if (character.IsDisciplineOnClanTriple(discipline.Id))
        {
            return "Clan (in-clan, ×4)";
        }

        if (character.IsDisciplineFromActiveBloodlineFourth(discipline.Id))
        {
            return "Bloodline (in-clan, ×4)";
        }

        if (discipline.IsNecromancy
            && string.Equals(character.Clan?.Name, "Mekhet", StringComparison.Ordinal)
            && !character.IsDisciplineOnClanTriple(discipline.Id))
        {
            return "Mekhet-associated (×5)";
        }

        return "Out-of-clan (×5)";
    }

    /// <summary>
    /// Extra tags for metadata-only disciplines (covenant-gated row, bloodline-only definition).
    /// </summary>
    public static string FormatDefinitionTags(Discipline? discipline)
    {
        if (discipline == null)
        {
            return string.Empty;
        }

        var tags = new List<string>();
        if (discipline.IsCovenantDiscipline)
        {
            tags.Add("Covenant Discipline");
        }

        if (discipline.IsBloodlineDiscipline)
        {
            tags.Add("Bloodline-only");
        }

        return tags.Count == 0 ? string.Empty : string.Join(" · ", tags);
    }

    /// <summary>
    /// Full option label for character creation discipline picker.
    /// </summary>
    public static string FormatCreationOption(Character character, Discipline discipline)
    {
        string affinity = character.IsDisciplineInClan(discipline.Id) ? "In-clan (×4)" : "Out-of-clan (×5)";
        var parts = new List<string> { discipline.Name, affinity };

        if (discipline.IsNecromancy
            && string.Equals(character.Clan?.Name, "Mekhet", StringComparison.Ordinal)
            && !character.IsDisciplineOnClanTriple(discipline.Id))
        {
            parts.Add("Mekhet-associated Necromancy");
        }

        string meta = FormatDefinitionTags(discipline);
        if (!string.IsNullOrEmpty(meta))
        {
            parts.Add(meta);
        }

        return string.Join(" — ", parts);
    }

    /// <summary>
    /// Label for advancement "add discipline" dropdown (includes XP tier and tags).
    /// </summary>
    public static string FormatAdvancementOption(Character character, Discipline discipline)
    {
        string affinity = character.IsDisciplineInClan(discipline.Id) ? "In-clan (×4)" : "Out-of-clan (×5)";
        var parts = new List<string> { discipline.Name, affinity };

        if (discipline.IsNecromancy
            && string.Equals(character.Clan?.Name, "Mekhet", StringComparison.Ordinal)
            && !character.IsDisciplineOnClanTriple(discipline.Id))
        {
            parts.Add("Mekhet-associated");
        }

        string meta = FormatDefinitionTags(discipline);
        if (!string.IsNullOrEmpty(meta))
        {
            parts.Add(meta);
        }

        return string.Join(" — ", parts);
    }
}
