using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain;

namespace RequiemNexus.Web.Helpers;

/// <summary>
/// Provides collection-based get/set for Character trait values.
/// Uses the Attributes and Skills collections instead of reflection.
/// </summary>
public static class CharacterTraitHelper
{
    /// <summary>
    /// Gets the integer value of an attribute from a Character.
    /// </summary>
    public static int GetTraitValue(Character character, AttributeId id)
        => character.GetAttributeRating(id);

    /// <summary>
    /// Gets the integer value of a skill from a Character.
    /// </summary>
    public static int GetTraitValue(Character character, SkillId id)
        => character.GetSkillRating(id);

    /// <summary>
    /// Gets the integer value of a trait (attribute or skill) by name.
    /// </summary>
    public static int GetTraitValue(Character character, string name)
    {
        if (Enum.TryParse<AttributeId>(name, out var attrId))
            return GetTraitValue(character, attrId);
        if (Enum.TryParse<SkillId>(name, out var skillId))
            return GetTraitValue(character, skillId);
        return 0;
    }

    /// <summary>
    /// Sets the integer value of an attribute on a Character.
    /// </summary>
    public static void SetTraitValue(Character character, AttributeId id, int value)
    {
        var attr = character.Attributes.FirstOrDefault(a => a.Name == id.ToString());
        if (attr != null)
        {
            attr.Rating = value;
        }
        else
        {
            character.Attributes.Add(new CharacterAttribute
            {
                Name = id.ToString(),
                Category = GetAttributeCategory(id),
                Rating = value,
            });
        }
    }

    /// <summary>
    /// Sets the integer value of a skill on a Character.
    /// </summary>
    public static void SetTraitValue(Character character, SkillId id, int value)
    {
        var skill = character.Skills.FirstOrDefault(s => s.Name == id.ToString());
        if (skill != null)
        {
            skill.Rating = value;
        }
        else
        {
            character.Skills.Add(new CharacterSkill
            {
                Name = id.ToString(),
                Category = GetSkillCategory(id),
                Rating = value,
            });
        }
    }

    /// <summary>
    /// Sets a trait value by string name (useful for generic UI components).
    /// </summary>
    public static void SetTraitValue(Character character, string name, int value)
    {
        if (Enum.TryParse<AttributeId>(name, out var attrId))
            SetTraitValue(character, attrId, value);
        else if (Enum.TryParse<SkillId>(name, out var skillId))
            SetTraitValue(character, skillId, value);
    }

    /// <summary>
    /// Seeds all 9 attributes on a new Character at rating 1.
    /// </summary>
    public static void SeedAttributes(Character character)
    {
        foreach (var id in TraitMetadata.MentalAttributes)
            character.Attributes.Add(CreateAttribute(id, TraitCategory.Mental, 1));
        foreach (var id in TraitMetadata.PhysicalAttributes)
            character.Attributes.Add(CreateAttribute(id, TraitCategory.Physical, 1));
        foreach (var id in TraitMetadata.SocialAttributes)
            character.Attributes.Add(CreateAttribute(id, TraitCategory.Social, 1));
    }

    /// <summary>
    /// Seeds all 24 skills on a new Character at rating 0.
    /// </summary>
    public static void SeedSkills(Character character)
    {
        foreach (var id in TraitMetadata.MentalSkills)
            character.Skills.Add(CreateSkill(id, TraitCategory.Mental, 0));
        foreach (var id in TraitMetadata.PhysicalSkills)
            character.Skills.Add(CreateSkill(id, TraitCategory.Physical, 0));
        foreach (var id in TraitMetadata.SocialSkills)
            character.Skills.Add(CreateSkill(id, TraitCategory.Social, 0));
    }

    private static CharacterAttribute CreateAttribute(AttributeId id, TraitCategory category, int rating)
    {
        return new CharacterAttribute
        {
            Name = id.ToString(),
            Category = category,
            Rating = rating,
        };
    }

    private static CharacterSkill CreateSkill(SkillId id, TraitCategory category, int rating)
    {
        return new CharacterSkill
        {
            Name = id.ToString(),
            Category = category,
            Rating = rating,
        };
    }

    private static TraitCategory GetAttributeCategory(AttributeId id)
    {
        if (TraitMetadata.MentalAttributes.Contains(id)) return TraitCategory.Mental;
        if (TraitMetadata.PhysicalAttributes.Contains(id)) return TraitCategory.Physical;
        return TraitCategory.Social;
    }

    private static TraitCategory GetSkillCategory(SkillId id)
    {
        if (TraitMetadata.MentalSkills.Contains(id)) return TraitCategory.Mental;
        if (TraitMetadata.PhysicalSkills.Contains(id)) return TraitCategory.Physical;
        return TraitCategory.Social;
    }
}
