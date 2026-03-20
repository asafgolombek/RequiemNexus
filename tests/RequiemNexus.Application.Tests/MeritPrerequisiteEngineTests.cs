using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain;
using RequiemNexus.Web.Helpers;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Unit tests for MeritPrerequisiteEngine validation logic.
/// </summary>
public class MeritPrerequisiteEngineTests
{
    private static Character BuildCharacter()
    {
        var c = new Character
        {
            ApplicationUserId = "test",
            Name = "Test",
            ClanId = 1,
            CreatureType = CreatureType.Vampire,
        };
        CharacterTraitHelper.SeedAttributes(c);
        CharacterTraitHelper.SeedSkills(c);
        return c;
    }

    [Fact]
    public void MeetsPrerequisites_EmptyList_ReturnsTrue()
    {
        var character = BuildCharacter();
        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, []));
        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, null!));
    }

    [Fact]
    public void MeetsPrerequisites_Attribute_Wits3_Satisfied()
    {
        var character = BuildCharacter();
        CharacterTraitHelper.SetTraitValue(character, "Wits", 3);

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Attribute,
                ReferenceId = (int)AttributeId.Wits,
                MinimumRating = 3,
                OrGroupId = 0,
            },
        };

        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_Attribute_Wits2_NotSatisfied()
    {
        var character = BuildCharacter();
        CharacterTraitHelper.SetTraitValue(character, "Wits", 2);

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Attribute,
                ReferenceId = (int)AttributeId.Wits,
                MinimumRating = 3,
                OrGroupId = 0,
            },
        };

        Assert.False(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_OrGroup_Wits3OrComposure3_WitsSatisfied()
    {
        var character = BuildCharacter();
        CharacterTraitHelper.SetTraitValue(character, "Wits", 3);
        CharacterTraitHelper.SetTraitValue(character, "Composure", 1);

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Attribute,
                ReferenceId = (int)AttributeId.Wits,
                MinimumRating = 3,
                OrGroupId = 1,
            },
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Attribute,
                ReferenceId = (int)AttributeId.Composure,
                MinimumRating = 3,
                OrGroupId = 2,
            },
        };

        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_OrGroup_Wits3OrComposure3_ComposureSatisfied()
    {
        var character = BuildCharacter();
        CharacterTraitHelper.SetTraitValue(character, "Wits", 1);
        CharacterTraitHelper.SetTraitValue(character, "Composure", 3);

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Attribute,
                ReferenceId = (int)AttributeId.Wits,
                MinimumRating = 3,
                OrGroupId = 1,
            },
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Attribute,
                ReferenceId = (int)AttributeId.Composure,
                MinimumRating = 3,
                OrGroupId = 2,
            },
        };

        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_OrGroup_NeitherSatisfied_ReturnsFalse()
    {
        var character = BuildCharacter();
        CharacterTraitHelper.SetTraitValue(character, "Wits", 2);
        CharacterTraitHelper.SetTraitValue(character, "Composure", 2);

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Attribute,
                ReferenceId = (int)AttributeId.Wits,
                MinimumRating = 3,
                OrGroupId = 1,
            },
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Attribute,
                ReferenceId = (int)AttributeId.Composure,
                MinimumRating = 3,
                OrGroupId = 2,
            },
        };

        Assert.False(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_MeritExclusion_HasExcludedMerit_ReturnsFalse()
    {
        var character = BuildCharacter();
        var merit = new Merit { Id = 10, Name = "Cutthroat" };
        character.Merits.Add(new CharacterMerit
        {
            MeritId = 10,
            Merit = merit,
            Rating = 1,
        });

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.MeritExclusion,
                ReferenceId = 10,
                MinimumRating = 0,
                OrGroupId = 0,
            },
        };

        Assert.False(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_MeritExclusion_DoesNotHaveExcludedMerit_ReturnsTrue()
    {
        var character = BuildCharacter();

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.MeritExclusion,
                ReferenceId = 10,
                MinimumRating = 0,
                OrGroupId = 0,
            },
        };

        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_MeritRequired_HasMeritAtRating_Satisfied()
    {
        var character = BuildCharacter();
        var merit = new Merit { Id = 20, Name = "Feeding Grounds" };
        character.Merits.Add(new CharacterMerit
        {
            MeritId = 20,
            Merit = merit,
            Rating = 3,
        });

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.MeritRequired,
                ReferenceId = 20,
                MinimumRating = 3,
                OrGroupId = 0,
            },
        };

        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_MeritRequired_DoesNotHaveMerit_ReturnsFalse()
    {
        var character = BuildCharacter();

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.MeritRequired,
                ReferenceId = 20,
                MinimumRating = 3,
                OrGroupId = 0,
            },
        };

        Assert.False(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_CreatureType_Vampire_Satisfied()
    {
        var character = BuildCharacter();
        character.CreatureType = CreatureType.Vampire;

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.CreatureType,
                ReferenceId = (int)CreatureType.Vampire,
                MinimumRating = 0,
                OrGroupId = 0,
            },
        };

        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }

    [Fact]
    public void MeetsPrerequisites_Clan_Matches_Satisfied()
    {
        var character = BuildCharacter();
        character.ClanId = 5;

        var prereqs = new List<MeritPrerequisite>
        {
            new()
            {
                PrerequisiteType = MeritPrerequisiteType.Clan,
                ReferenceId = 5,
                MinimumRating = 0,
                OrGroupId = 0,
            },
        };

        Assert.True(MeritPrerequisiteEngine.MeetsPrerequisites(character, prereqs));
    }
}
