using System.Linq;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class TraitMetadataTests
{
    [Theory]
    [InlineData(AttributeId.Intelligence, "Intelligence")]
    [InlineData(SkillId.AnimalKen, "Animal Ken")]
    [InlineData(SkillId.Firearms, "Firearms")]
    public void GetDisplayName_HandlesDescriptionsCorrectly(Enum input, string expected)
    {
        string actual = input switch
        {
            AttributeId a => TraitMetadata.GetDisplayName(a),
            SkillId s => TraitMetadata.GetDisplayName(s),
            _ => input.ToString()
        };
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MentalAttributes_ContainsCorrectTraits()
    {
        Assert.Contains(AttributeId.Intelligence, TraitMetadata.MentalAttributes);
        Assert.Contains(AttributeId.Wits, TraitMetadata.MentalAttributes);
        Assert.Contains(AttributeId.Resolve, TraitMetadata.MentalAttributes);
        Assert.Equal(3, TraitMetadata.MentalAttributes.Length);
    }

    [Fact]
    public void AllAttributes_IsUnionOfCategories()
    {
        int expectedCount = TraitMetadata.MentalAttributes.Length +
            TraitMetadata.PhysicalAttributes.Length +
            TraitMetadata.SocialAttributes.Length;

        Assert.Equal(expectedCount, TraitMetadata.AllAttributes.Length);
    }

    [Theory]
    [InlineData(AttributeId.Intelligence, true)]
    [InlineData(AttributeId.Strength, true)]
    [InlineData(AttributeId.Presence, true)]
    [InlineData(SkillId.Academics, false)]
    [InlineData(SkillId.Brawl, false)]
    public void IsAttribute_IdentifiesCorrectTraits(Enum trait, bool expected)
    {
        bool actual = trait switch
        {
            AttributeId a => TraitMetadata.IsAttribute(a),
            SkillId s => TraitMetadata.IsAttribute(s),
            _ => false
        };
        Assert.Equal(expected, actual);
    }
}
