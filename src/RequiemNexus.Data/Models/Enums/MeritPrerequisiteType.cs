namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// Type of prerequisite required to purchase a merit.
/// ReferenceId maps to: MeritId (MeritRequired/MeritExclusion), AttributeId (Attribute),
/// SkillId (Skill), DisciplineId (Discipline), ClanId (Clan), CreatureType enum value (CreatureType).
/// </summary>
public enum MeritPrerequisiteType
{
    /// <summary>Character must have Merit with ReferenceId and Rating >= MinimumRating.</summary>
    MeritRequired,

    /// <summary>Character must NOT have any merit with ReferenceId (exclusion).</summary>
    MeritExclusion,

    /// <summary>Character attribute (ReferenceId = AttributeId) >= MinimumRating.</summary>
    Attribute,

    /// <summary>Character skill (ReferenceId = SkillId) >= MinimumRating.</summary>
    Skill,

    /// <summary>Character discipline (ReferenceId = DisciplineId) >= MinimumRating.</summary>
    Discipline,

    /// <summary>Character CreatureType must match ReferenceId (enum value).</summary>
    CreatureType,

    /// <summary>Character ClanId must match ReferenceId.</summary>
    Clan,

    /// <summary>Character Title must match (ReferenceId encodes title; future use).</summary>
    Title,
}
