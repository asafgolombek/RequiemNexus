using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Resolves a <see cref="PoolDefinition"/> into a dice pool integer by hydrating each trait
/// from the character's ratings. Phase 9: supports additive pools, penalty dice, lower-of, and modifiers.
/// </summary>
public class TraitResolver(IModifierService modifierService) : ITraitResolver
{
    /// <inheritdoc />
    public int ResolvePool(Character character, PoolDefinition pool)
    {
        int total = 0;

        foreach (var trait in pool.Traits)
        {
            total += ResolveTrait(character, trait);
        }

        if (pool.LowerOf is { } lowerOf)
        {
            int left = ResolveTrait(character, lowerOf.Left);
            int right = ResolveTrait(character, lowerOf.Right);
            total += Math.Min(left, right);
        }

        if (pool.PenaltyTraits is { } penaltyTraits)
        {
            foreach (var trait in penaltyTraits)
            {
                total -= ResolveTrait(character, trait);
            }
        }

        return Math.Max(0, total);
    }

    /// <inheritdoc />
    public async Task<int> ResolvePoolAsync(Character character, PoolDefinition pool)
    {
        int basePool = ResolvePool(character, pool);

        var targets = new HashSet<ModifierTarget>();
        foreach (var trait in pool.Traits)
        {
            if (GetModifierTargetForTrait(trait) is { } t)
            {
                targets.Add(t);
            }
        }

        if (pool.LowerOf is { } lowerOf)
        {
            if (GetModifierTargetForTrait(lowerOf.Left) is { } lt)
            {
                targets.Add(lt);
            }

            if (GetModifierTargetForTrait(lowerOf.Right) is { } rt)
            {
                targets.Add(rt);
            }
        }

        var modifiers = await modifierService.GetModifiersForCharacterAsync(character.Id);
        int delta = modifiers
            .Where(m => targets.Contains(m.Target) && m.ModifierType != ModifierType.RuleBreaking)
            .Sum(m => m.Value);

        return Math.Max(0, basePool + delta);
    }

    private static ModifierTarget? GetModifierTargetForTrait(TraitReference trait)
    {
        if (trait.Type == TraitType.Skill && trait.SkillId.HasValue)
        {
            return trait.SkillId.Value switch
            {
                SkillId.Brawl => ModifierTarget.Brawl,
                SkillId.Athletics => ModifierTarget.Athletics,
                SkillId.Weaponry => ModifierTarget.Weaponry,
                SkillId.Firearms => ModifierTarget.Firearms,
                _ => null,
            };
        }

        return null;
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
