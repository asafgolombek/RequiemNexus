using System.Collections.Frozen;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Resolves a <see cref="PoolDefinition"/> into a dice pool integer by hydrating each trait
/// from the character's ratings. Phase 9: supports additive pools, penalty dice, lower-of, and modifiers.
/// </summary>
public class TraitResolver(IModifierService modifierService) : ITraitResolver
{
    private static readonly FrozenDictionary<TraitType, Func<Character, TraitReference, int>> _traitRatingResolvers =
        new Dictionary<TraitType, Func<Character, TraitReference, int>>
        {
            [TraitType.Attribute] = static (c, t) =>
                t.AttributeId.HasValue ? c.GetAttributeRating(t.AttributeId.Value) : 0,
            [TraitType.Skill] = static (c, t) =>
                t.SkillId.HasValue ? c.GetSkillRating(t.SkillId.Value) : 0,
            [TraitType.Discipline] = static (c, t) =>
                t.DisciplineId.HasValue ? c.GetDisciplineRating(t.DisciplineId.Value) : 0,
        }.ToFrozenDictionary();

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
        HashSet<AttributeId> poolAttributes = CollectPoolAttributeIds(pool);

        var modifiers = await modifierService.GetModifiersForCharacterAsync(character.Id);
        int delta = 0;
        int equipmentSkillBonusSum = 0;

        foreach (var m in modifiers)
        {
            if (m.ModifierType == ModifierType.RuleBreaking)
            {
                continue;
            }

            if (TryApplyConditionDicePoolModifier(m, poolSkills, poolAttributes, ref delta))
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

    private static bool PoolIncludesMentalSkill(HashSet<SkillId> poolSkills)
    {
        foreach (SkillId s in TraitMetadata.MentalSkills)
        {
            if (poolSkills.Contains(s))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies condition-sourced dice pool penalties (Phase 17). Returns <c>true</c> when <paramref name="m"/> is fully handled here.
    /// </summary>
    private static bool TryApplyConditionDicePoolModifier(
        PassiveModifier m,
        HashSet<SkillId> poolSkills,
        HashSet<AttributeId> poolAttributes,
        ref int delta)
    {
        switch (m.Target)
        {
            case ModifierTarget.AllDicePools:
            case ModifierTarget.DicePoolsExceptFleeing:
                delta += m.Value;
                return true;
            case ModifierTarget.PhysicalDicePools:
                if (PoolIncludesPhysicalSkill(poolSkills))
                {
                    delta += m.Value;
                }

                return true;
            case ModifierTarget.MentalDicePools:
                if (PoolIncludesMentalSkill(poolSkills))
                {
                    delta += m.Value;
                }

                return true;
            case ModifierTarget.PoolsUsingResolveOrComposure:
                if (poolAttributes.Contains(AttributeId.Resolve) || poolAttributes.Contains(AttributeId.Composure))
                {
                    delta += m.Value;
                }

                return true;
            case ModifierTarget.PoolsUsingComposureAttribute:
                if (poolAttributes.Contains(AttributeId.Composure))
                {
                    delta += m.Value;
                }

                return true;
            default:
                return false;
        }
    }

    private static HashSet<AttributeId> CollectPoolAttributeIds(PoolDefinition pool)
    {
        var set = new HashSet<AttributeId>();
        foreach (var trait in pool.Traits)
        {
            AddAttributeFromTrait(set, trait);
        }

        if (pool.LowerOf is { } lowerOf)
        {
            AddAttributeFromTrait(set, lowerOf.Left);
            AddAttributeFromTrait(set, lowerOf.Right);
        }

        if (pool.PenaltyTraits is { } penalties)
        {
            foreach (var trait in penalties)
            {
                AddAttributeFromTrait(set, trait);
            }
        }

        return set;
    }

    private static void AddAttributeFromTrait(HashSet<AttributeId> set, TraitReference trait)
    {
        if (trait.Type == TraitType.Attribute && trait.AttributeId.HasValue)
        {
            set.Add(trait.AttributeId.Value);
        }
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

    private static int ResolveTrait(Character character, TraitReference trait) =>
        _traitRatingResolvers.GetValueOrDefault(trait.Type)?.Invoke(character, trait) ?? 0;

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
