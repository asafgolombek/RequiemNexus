using Microsoft.EntityFrameworkCore;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for TraitResolver — verifies correct hydration of Attribute, Skill, and Discipline pools.
/// </summary>
public class TraitResolverTests
{
    private static Character CreateCharacterWithTraits()
    {
        var character = new Character
        {
            Id = 1,
            Name = "Test",
            Attributes =
            [
                new CharacterAttribute { Name = "Stamina", Rating = 4 },
                new CharacterAttribute { Name = "Wits", Rating = 3 },
                new CharacterAttribute { Name = "Intelligence", Rating = 2 },
            ],
            Skills =
            [
                new CharacterSkill { Name = "Athletics", Rating = 2 },
                new CharacterSkill { Name = "Survival", Rating = 3 },
                new CharacterSkill { Name = "Occult", Rating = 1 },
            ],
            Disciplines =
            [
                new CharacterDiscipline { DisciplineId = 1, Rating = 3 },
                new CharacterDiscipline { DisciplineId = 2, Rating = 2 },
            ],
        };

        // Attach disciplines to character for navigation
        foreach (var d in character.Disciplines)
        {
            d.Character = character;
        }

        return character;
    }

    private static TraitResolver CreateTraitResolver()
    {
        var modifierServiceMock = new Mock<IModifierService>();
        modifierServiceMock.Setup(m => m.GetModifiersForCharacterAsync(It.IsAny<int>()))
            .ReturnsAsync(Array.Empty<PassiveModifier>());
        return new TraitResolver(modifierServiceMock.Object);
    }

    [Fact]
    public void ResolvePool_AttributePlusSkillPlusDiscipline_SumsCorrectly()
    {
        var character = CreateCharacterWithTraits();
        character.Disciplines.First(d => d.DisciplineId == 1).Discipline = new Discipline { Id = 1, Name = "Vigor" };
        character.Disciplines.First(d => d.DisciplineId == 2).Discipline = new Discipline { Id = 2, Name = "Resilience" };

        var pool = new PoolDefinition(
        [
            new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
            new TraitReference(TraitType.Skill, null, SkillId.Athletics, null),
            new TraitReference(TraitType.Discipline, null, null, 1),
        ]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(4 + 2 + 3, result);
    }

    [Fact]
    public void ResolvePool_EmptyTraits_ReturnsZero()
    {
        var character = CreateCharacterWithTraits();
        var pool = new PoolDefinition([]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ResolvePool_MissingDiscipline_ReturnsZeroForThatTrait()
    {
        var character = CreateCharacterWithTraits();
        character.Disciplines.Clear();

        var pool = new PoolDefinition(
        [
            new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
            new TraitReference(TraitType.Discipline, null, null, 99),
        ]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(4 + 0, result);
    }

    [Fact]
    public void ResolvePool_WithPenaltyTraits_SubtractsAfterSum()
    {
        var character = CreateCharacterWithTraits();

        var pool = new PoolDefinition(
            Traits:
            [
                new TraitReference(TraitType.Attribute, AttributeId.Wits, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Athletics, null),
            ],
            PenaltyTraits:
            [
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
            ]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal((3 + 2) - 4, result);
    }

    [Fact]
    public void ResolvePool_WithLowerOf_UsesMinimumOfTwoTraits()
    {
        var character = CreateCharacterWithTraits();
        character.Disciplines.Add(new CharacterDiscipline { DisciplineId = 3, Rating = 5 });
        character.Disciplines.First(d => d.DisciplineId == 1).Rating = 2;

        var pool = new PoolDefinition(
            Traits:
            [
                new TraitReference(TraitType.Attribute, AttributeId.Intelligence, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Occult, null),
            ],
            LowerOf: new LowerOfPair(
                new TraitReference(TraitType.Discipline, null, null, 1),
                new TraitReference(TraitType.Discipline, null, null, 3)));

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(2 + 1 + Math.Min(2, 5), result);
    }

    [Fact]
    public void ResolvePool_ContestedAgainst_DoesNotAffectCasterPool()
    {
        var character = CreateCharacterWithTraits();
        var contestedPool = new PoolDefinition(
            [new TraitReference(TraitType.Attribute, AttributeId.Resolve, null, null), new TraitReference(TraitType.Attribute, AttributeId.Composure, null, null)]);

        var pool = new PoolDefinition(
            Traits:
            [
                new TraitReference(TraitType.Attribute, AttributeId.Intelligence, null, null),
                new TraitReference(TraitType.Skill, null, SkillId.Occult, null),
            ],
            ContestedAgainst: contestedPool);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(2 + 1, result);
    }

    [Fact]
    public void ResolvePool_PenaltyExceedsSum_ReturnsZero()
    {
        var character = CreateCharacterWithTraits();

        var pool = new PoolDefinition(
            Traits: [new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null)],
            PenaltyTraits:
            [
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
                new TraitReference(TraitType.Attribute, AttributeId.Stamina, null, null),
            ]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ResolvePoolAsync_SkillPoolEquipmentBonus_AppliesWhenSkillInPool_CappedPerBook()
    {
        var character = CreateCharacterWithTraits();
        character.Skills.Add(new CharacterSkill { Name = "Investigation", Rating = 1 });
        var pool = new PoolDefinition(
        [
            new TraitReference(TraitType.Attribute, AttributeId.Intelligence, null, null),
            new TraitReference(TraitType.Skill, null, SkillId.Investigation, null),
        ]);

        var mockMods = new Mock<IModifierService>();
        mockMods.Setup(m => m.GetModifiersForCharacterAsync(character.Id))
            .ReturnsAsync(
            [
                new PassiveModifier(
                    ModifierTarget.SkillPool,
                    2,
                    ModifierType.Static,
                    "Crime Scene Kit",
                    new ModifierSource(ModifierSourceType.Equipment, 99))
                {
                    AppliesToSkill = SkillId.Investigation,
                },
            ]);

        var resolver = new TraitResolver(mockMods.Object);
        int result = await resolver.ResolvePoolAsync(character, pool);

        Assert.Equal(2 + 1 + 2, result);
    }

    [Fact]
    public void ResolvePool_UntrainedPhysicalSkill_SubtractsOneDie()
    {
        var character = CreateCharacterWithTraits();
        character.Attributes.Add(new CharacterAttribute { Name = "Strength", Rating = 3 });
        var pool = new PoolDefinition(
        [
            new TraitReference(TraitType.Attribute, AttributeId.Strength, null, null),
            new TraitReference(TraitType.Skill, null, SkillId.Brawl, null),
        ]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(2, result);
    }

    [Fact]
    public void ResolvePool_MentalUntrained_SubtractsThreeDice()
    {
        var character = CreateCharacterWithTraits();
        var pool = new PoolDefinition(
        [
            new TraitReference(TraitType.Attribute, AttributeId.Intelligence, null, null),
            new TraitReference(TraitType.Skill, null, SkillId.Investigation, null),
        ]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ResolvePool_MentalUntrained_LeavesPositivePoolWhenAttributeHighEnough()
    {
        var character = CreateCharacterWithTraits();
        character.Attributes.First(a => a.Name == "Intelligence").Rating = 5;
        var pool = new PoolDefinition(
        [
            new TraitReference(TraitType.Attribute, AttributeId.Intelligence, null, null),
            new TraitReference(TraitType.Skill, null, SkillId.Investigation, null),
        ]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(2, result);
    }

    [Fact]
    public void ResolvePool_TwoUntrainedPhysicalSkills_SubtractsTwoDice()
    {
        var character = new Character
        {
            Id = 1,
            Name = "Test",
            Attributes = [new CharacterAttribute { Name = "Strength", Rating = 3 }],
            Skills = [],
        };

        var pool = new PoolDefinition(
        [
            new TraitReference(TraitType.Skill, null, SkillId.Brawl, null),
            new TraitReference(TraitType.Skill, null, SkillId.Weaponry, null),
        ]);

        var resolver = CreateTraitResolver();
        int result = resolver.ResolvePool(character, pool);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ResolvePoolAsync_EquipmentSkillPoolBonuses_SumCappedAtFive()
    {
        var character = CreateCharacterWithTraits();
        character.Skills.Add(new CharacterSkill { Name = "Investigation", Rating = 1 });
        var pool = new PoolDefinition(
        [
            new TraitReference(TraitType.Skill, null, SkillId.Investigation, null),
        ]);

        var mockMods = new Mock<IModifierService>();
        mockMods.Setup(m => m.GetModifiersForCharacterAsync(character.Id))
            .ReturnsAsync(
            [
                new PassiveModifier(
                    ModifierTarget.SkillPool,
                    3,
                    ModifierType.Static,
                    "A",
                    new ModifierSource(ModifierSourceType.Equipment, 1))
                {
                    AppliesToSkill = SkillId.Investigation,
                },
                new PassiveModifier(
                    ModifierTarget.SkillPool,
                    3,
                    ModifierType.Static,
                    "B",
                    new ModifierSource(ModifierSourceType.Equipment, 2))
                {
                    AppliesToSkill = SkillId.Investigation,
                },
            ]);

        var resolver = new TraitResolver(mockMods.Object);
        int result = await resolver.ResolvePoolAsync(character, pool);

        Assert.Equal(1 + 5, result); // skill 1 + equipment cap 5 (3+3 uncapped would be 6)
    }
}
