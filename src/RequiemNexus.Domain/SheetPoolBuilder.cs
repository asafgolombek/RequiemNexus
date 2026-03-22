using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Domain;

/// <summary>
/// Builds <see cref="PoolDefinition"/> instances from sheet UI labels (display names or enum names).
/// </summary>
public static class SheetPoolBuilder
{
    /// <summary>
    /// Parses a trait label into a <see cref="TraitReference"/>.
    /// </summary>
    /// <param name="label">Display label or compact enum name (spaces stripped).</param>
    /// <returns>null when the label does not map to a known attribute or skill.</returns>
    public static TraitReference? TryTraitFromLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        string compact = label.Replace(" ", string.Empty);
        if (TraitMetadata.IsAttribute(compact) && Enum.TryParse<AttributeId>(compact, out AttributeId attr))
        {
            return new TraitReference(TraitType.Attribute, attr, null, null);
        }

        if (Enum.TryParse<SkillId>(compact, out SkillId skill))
        {
            return new TraitReference(TraitType.Skill, null, skill, null);
        }

        return null;
    }

    /// <summary>
    /// Builds a pool from a primary trait and optional associated trait (attribute + skill, etc.).
    /// </summary>
    public static PoolDefinition? TryCreate(string? primaryLabel, string? associatedLabel)
    {
        TraitReference? primary = TryTraitFromLabel(primaryLabel);
        if (primary == null)
        {
            return null;
        }

        var traits = new List<TraitReference> { primary };
        TraitReference? secondary = TryTraitFromLabel(associatedLabel);
        if (secondary != null)
        {
            traits.Add(secondary);
        }

        return new PoolDefinition(traits);
    }
}
