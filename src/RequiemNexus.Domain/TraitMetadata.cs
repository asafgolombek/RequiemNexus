using System.ComponentModel;

namespace RequiemNexus.Domain;

/// <summary>
/// Centralized metadata for all Vampire: The Requiem character traits.
/// Replaces string-based identifiers with enums to reduce conversion friction and scattering.
/// </summary>
public static class TraitMetadata
{
    // --- Attributes ---
    public static readonly AttributeId[] MentalAttributes =
        [AttributeId.Intelligence, AttributeId.Wits, AttributeId.Resolve];

    public static readonly AttributeId[] PhysicalAttributes =
        [AttributeId.Strength, AttributeId.Dexterity, AttributeId.Stamina];

    public static readonly AttributeId[] SocialAttributes =
        [AttributeId.Presence, AttributeId.Manipulation, AttributeId.Composure];

    public static readonly AttributeId[] AllAttributes =
        [.. MentalAttributes, .. PhysicalAttributes, .. SocialAttributes];

    // --- Skills ---
    public static readonly SkillId[] MentalSkills =
        [SkillId.Academics, SkillId.Computer, SkillId.Crafts, SkillId.Investigation, SkillId.Medicine, SkillId.Occult, SkillId.Politics, SkillId.Science];

    public static readonly SkillId[] PhysicalSkills =
        [SkillId.Athletics, SkillId.Brawl, SkillId.Drive, SkillId.Firearms, SkillId.Larceny, SkillId.Stealth, SkillId.Survival, SkillId.Weaponry];

    public static readonly SkillId[] SocialSkills =
        [SkillId.AnimalKen, SkillId.Empathy, SkillId.Expression, SkillId.Intimidation, SkillId.Persuasion, SkillId.Socialize, SkillId.Streetwise, SkillId.Subterfuge];

    public static readonly SkillId[] AllSkills =
        [.. MentalSkills, .. PhysicalSkills, .. SocialSkills];

    /// <summary>
    /// Returns true if the skill is a Mental skill on the VtR character sheet (untrained penalty −3 dice in pool resolution).
    /// </summary>
    public static bool IsMentalSkill(SkillId id) => MentalSkills.AsSpan().Contains(id);

    /// <summary>
    /// Returns the display name for an Attribute.
    /// </summary>
    public static string GetDisplayName(AttributeId id) => GetEnumDescription(id);

    /// <summary>
    /// Returns the display name for a Skill.
    /// </summary>
    public static string GetDisplayName(SkillId id) => GetEnumDescription(id);

    /// <summary>
    /// Returns the display name for a trait by string name.
    /// </summary>
    public static string GetDisplayName(string name)
    {
        if (Enum.TryParse<AttributeId>(name, out var attrId))
        {
            return GetDisplayName(attrId);
        }

        if (Enum.TryParse<SkillId>(name, out var skillId))
        {
            return GetDisplayName(skillId);
        }

        return name;
    }

    /// <summary>
    /// Returns true if the given trait name is an Attribute.
    /// </summary>
    public static bool IsAttribute(string name)
    {
        return Enum.TryParse<AttributeId>(name, out _);
    }

    /// <summary>
    /// Returns true if the given trait is an Attribute.
    /// </summary>
    public static bool IsAttribute(AttributeId id) => true;

    /// <summary>
    /// Returns false for Skills.
    /// </summary>
    public static bool IsAttribute(SkillId id) => false;

    private static string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null)
        {
            return value.ToString();
        }

        var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute?.Description ?? value.ToString();
    }
}
