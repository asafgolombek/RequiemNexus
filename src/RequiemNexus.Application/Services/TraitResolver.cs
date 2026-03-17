using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Resolves a <see cref="PoolDefinition"/> into a dice pool integer by hydrating each trait
/// from the character's ratings. Phase 8: additive pools only.
/// </summary>
public class TraitResolver : ITraitResolver
{
    /// <inheritdoc />
    public int ResolvePool(Character character, PoolDefinition pool)
    {
        if (pool.Traits.Count == 0)
        {
            return 0;
        }

        int total = 0;
        foreach (var trait in pool.Traits)
        {
            total += ResolveTrait(character, trait);
        }

        return Math.Max(0, total);
    }

    private static int ResolveTrait(Character character, TraitReference trait)
    {
        return trait.Type switch
        {
            TraitType.Attribute => trait.AttributeId.HasValue
                ? character.GetAttributeRating(trait.AttributeId!.Value)
                : 0,
            TraitType.Skill => trait.SkillId.HasValue
                ? character.GetSkillRating(trait.SkillId!.Value)
                : 0,
            TraitType.Discipline => trait.DisciplineId.HasValue
                ? character.GetDisciplineRating(trait.DisciplineId!.Value)
                : 0,
            _ => 0,
        };
    }
}
