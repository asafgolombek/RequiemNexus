using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Validates whether a character meets the structured prerequisites for a merit.
/// OR-group logic: satisfy all prerequisites in at least one OrGroupId (same pattern as DevotionService).
/// </summary>
public static class MeritPrerequisiteEngine
{
    /// <summary>
    /// Returns true if the character meets all prerequisites. Empty prerequisites list returns true.
    /// </summary>
    public static bool MeetsPrerequisites(Character character, IReadOnlyList<MeritPrerequisite> prerequisites)
    {
        if (prerequisites == null || prerequisites.Count == 0)
        {
            return true;
        }

        // MeritExclusion: character must NOT have any of the excluded merits. Check first.
        var exclusions = prerequisites.Where(p => p.PrerequisiteType == MeritPrerequisiteType.MeritExclusion).ToList();
        foreach (var excl in exclusions)
        {
            if (excl.ReferenceId.HasValue && CharacterHasMerit(character, excl.ReferenceId.Value, excl.MinimumRating))
            {
                return false;
            }
        }

        // Remaining prerequisites use OR-group logic: satisfy ALL in at least one group.
        var nonExclusion = prerequisites.Where(p => p.PrerequisiteType != MeritPrerequisiteType.MeritExclusion).ToList();
        if (nonExclusion.Count == 0)
        {
            return true;
        }

        var groups = nonExclusion.GroupBy(p => p.OrGroupId);
        foreach (var group in groups)
        {
            bool groupSatisfied = true;
            foreach (var prereq in group)
            {
                if (!SatisfiesPrerequisite(character, prereq))
                {
                    groupSatisfied = false;
                    break;
                }
            }

            if (groupSatisfied)
            {
                return true;
            }
        }

        return false;
    }

    private static bool SatisfiesPrerequisite(Character character, MeritPrerequisite prereq)
    {
        return prereq.PrerequisiteType switch
        {
            MeritPrerequisiteType.MeritRequired => prereq.ReferenceId.HasValue &&
                CharacterHasMerit(character, prereq.ReferenceId.Value, prereq.MinimumRating),
            MeritPrerequisiteType.MeritExclusion => true, // Handled above
            MeritPrerequisiteType.Attribute => prereq.ReferenceId.HasValue &&
                character.GetAttributeRating((AttributeId)prereq.ReferenceId.Value) >= prereq.MinimumRating,
            MeritPrerequisiteType.Skill => prereq.ReferenceId.HasValue &&
                character.GetSkillRating((SkillId)prereq.ReferenceId.Value) >= prereq.MinimumRating,
            MeritPrerequisiteType.Discipline => prereq.ReferenceId.HasValue &&
                character.GetDisciplineRating(prereq.ReferenceId.Value) >= prereq.MinimumRating,
            MeritPrerequisiteType.CreatureType => prereq.ReferenceId.HasValue &&
                (int)character.CreatureType == prereq.ReferenceId.Value,
            MeritPrerequisiteType.Clan => prereq.ReferenceId.HasValue &&
                character.ClanId == prereq.ReferenceId.Value,
            MeritPrerequisiteType.Title => false, // Not yet implemented
            _ => false,
        };
    }

    private static bool CharacterHasMerit(Character character, int meritId, int minimumRating)
    {
        var cm = character.Merits.FirstOrDefault(m => m.MeritId == meritId);
        return cm != null && cm.Rating >= minimumRating;
    }
}
