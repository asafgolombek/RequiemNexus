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
    /// Gets the integer value of a trait from a Character by name.
    /// </summary>
    public static int GetTraitValue(Character character, string traitName)
    {
        if (TraitMetadata.IsAttribute(traitName))
            return character.GetAttributeRating(traitName);
        else
            return character.GetSkillRating(traitName);
    }

    /// <summary>
    /// Sets the integer value of a trait on a Character by name.
    /// Creates the trait entry if it doesn't exist yet (for character creation).
    /// </summary>
    public static void SetTraitValue(Character character, string traitName, int value)
    {
        if (TraitMetadata.IsAttribute(traitName))
        {
            var attr = character.Attributes.FirstOrDefault(a => a.Name == traitName);
            if (attr != null)
                attr.Rating = value;
            else
                character.Attributes.Add(new CharacterAttribute
                {
                    Name = traitName,
                    Category = GetAttributeCategory(traitName),
                    Rating = value
                });
        }
        else
        {
            var skill = character.Skills.FirstOrDefault(s => s.Name == traitName);
            if (skill != null)
                skill.Rating = value;
            else
                character.Skills.Add(new CharacterSkill
                {
                    Name = traitName,
                    Category = GetSkillCategory(traitName),
                    Rating = value
                });
        }
    }

    /// <summary>
    /// Seeds all 9 attributes on a new Character at rating 1.
    /// </summary>
    public static void SeedAttributes(Character character)
    {
        foreach (var (category, names) in new[]
        {
            (TraitCategory.Mental, TraitMetadata.MentalAttributes),
            (TraitCategory.Physical, TraitMetadata.PhysicalAttributes),
            (TraitCategory.Social, TraitMetadata.SocialAttributes)
        })
        {
            foreach (var name in names)
            {
                character.Attributes.Add(new CharacterAttribute
                {
                    Name = name,
                    Category = category,
                    Rating = 1
                });
            }
        }
    }

    /// <summary>
    /// Seeds all 24 skills on a new Character at rating 0.
    /// </summary>
    public static void SeedSkills(Character character)
    {
        foreach (var (category, names) in new[]
        {
            (TraitCategory.Mental, TraitMetadata.MentalSkills),
            (TraitCategory.Physical, TraitMetadata.PhysicalSkills),
            (TraitCategory.Social, TraitMetadata.SocialSkills)
        })
        {
            foreach (var name in names)
            {
                character.Skills.Add(new CharacterSkill
                {
                    Name = name,
                    Category = category,
                    Rating = 0
                });
            }
        }
    }

    private static TraitCategory GetAttributeCategory(string name)
    {
        if (TraitMetadata.MentalAttributes.Contains(name)) return TraitCategory.Mental;
        if (TraitMetadata.PhysicalAttributes.Contains(name)) return TraitCategory.Physical;
        return TraitCategory.Social;
    }

    private static TraitCategory GetSkillCategory(string name)
    {
        if (TraitMetadata.MentalSkills.Contains(name)) return TraitCategory.Mental;
        if (TraitMetadata.PhysicalSkills.Contains(name)) return TraitCategory.Physical;
        return TraitCategory.Social;
    }
}

