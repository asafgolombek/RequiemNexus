using System.Linq;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class TraitMetadataTests
{
    [Theory]
    [InlineData("Intelligence", "Intelligence")]
    [InlineData("AnimalKen", "Animal Ken")]
    [InlineData("Firearms", "Firearms")]
    public void GetDisplayName_HandlesCamelCaseCorrectly(string input, string expected)
    {
        string actual = TraitMetadata.GetDisplayName(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MentalAttributes_ContainsCorrectTraits()
    {
        Assert.Contains("Intelligence", TraitMetadata.MentalAttributes);
        Assert.Contains("Wits", TraitMetadata.MentalAttributes);
        Assert.Contains("Resolve", TraitMetadata.MentalAttributes);
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
    [InlineData("Intelligence", true)]
    [InlineData("Strength", true)]
    [InlineData("Presence", true)]
    [InlineData("Academics", false)]
    [InlineData("Brawl", false)]
    public void IsAttribute_IdentifiesCorrectTraits(string trait, bool expected)
    {
        Assert.Equal(expected, TraitMetadata.IsAttribute(trait));
    }
}
