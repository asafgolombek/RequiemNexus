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

        total -= CountUntrainedSkillDicePenalty(character, pool);

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

        HashSet<SkillId> poolSkills = CollectPoolSkillIds(pool);

        var modifiers = await modifierService.GetModifiersForCharacterAsync(character.Id);
        int delta = 0;
        int equipmentSkillBonusSum = 0;

        foreach (var m in modifiers)
        {
            if (m.ModifierType == ModifierType.RuleBreaking)
            {
                continue;
            }

            if (m.Target == ModifierTarget.WoundPenalty)
            {
                if (PoolIncludesPhysicalSkill(poolSkills))
                {
                    delta += m.Value;
                }

                continue;
            }

            if (m.Target == ModifierTarget.SkillPool
                && m.AppliesToSkill.HasValue
                && poolSkills.Contains(m.AppliesToSkill.Value))
            {
                if (m.Source.SourceType == ModifierSourceType.Equipment)
                {
                    equipmentSkillBonusSum += m.Value;
                }
                else
                {
                    delta += m.Value;
                }

                continue;
            }

            if (m.Target == ModifierTarget.SkillPool)
            {
                continue;
            }

            if (targets.Contains(m.Target))
            {
                delta += m.Value;
            }
        }

        equipmentSkillBonusSum = Math.Min(equipmentSkillBonusSum, 5);
        delta += equipmentSkillBonusSum;

        return Math.Max(0, basePool + delta);
    }

    private static bool PoolIncludesPhysicalSkill(HashSet<SkillId> poolSkills)
    {
        foreach (SkillId s in TraitMetadata.PhysicalSkills)
        {
            if (poolSkills.Contains(s))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<SkillId> CollectPoolSkillIds(PoolDefinition pool)
    {
        var set = new HashSet<SkillId>();
        foreach (var trait in pool.Traits)
        {
            AddSkillFromTrait(set, trait);
        }

        if (pool.LowerOf is { } lowerOf)
        {
            AddSkillFromTrait(set, lowerOf.Left);
            AddSkillFromTrait(set, lowerOf.Right);
        }

        if (pool.PenaltyTraits is { } penalties)
        {
            foreach (var trait in penalties)
            {
                AddSkillFromTrait(set, trait);
            }
        }

        return set;
    }

    private static void AddSkillFromTrait(HashSet<SkillId> set, TraitReference trait)
    {
        if (trait.Type == TraitType.Skill && trait.SkillId.HasValue)
        {
            set.Add(trait.SkillId.Value);
        }
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

    /// <summary>
    /// VtR-style untrained skills: Mental skills at 0 dots apply −3 dice; Physical and Social at 0 apply −1 each (distinct skills in pool).
    /// </summary>
    private static int CountUntrainedSkillDicePenalty(Character character, PoolDefinition pool)
    {
        int penalty = 0;
        foreach (SkillId skillId in CollectPoolSkillIds(pool))
        {
            if (character.GetSkillRating(skillId) != 0)
            {
                continue;
            }

            penalty += TraitMetadata.IsMentalSkill(skillId) ? 3 : 1;
        }

        return penalty;
    }
}
