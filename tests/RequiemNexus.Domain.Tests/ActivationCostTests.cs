using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Unit tests for <see cref="ActivationCost.Parse"/>.
/// </summary>
public class ActivationCostTests
{
    [Theory]
    [InlineData("1 Vitae", ActivationCostType.Vitae, 1)]
    [InlineData("2 Vitae", ActivationCostType.Vitae, 2)]
    [InlineData("1 Willpower", ActivationCostType.Willpower, 1)]
    [InlineData("1 VITAE", ActivationCostType.Vitae, 1)]
    public void Parse_RecognisedStrings_ReturnsExpectedTypeAndAmount(string input, ActivationCostType expectedType, int expectedAmount)
    {
        ActivationCost cost = ActivationCost.Parse(input);
        Assert.Equal(expectedType, cost.Type);
        Assert.Equal(expectedAmount, cost.Amount);
    }

    [Fact]
    public void Parse_VitaeOrWillpower_SetsPlayerChoiceFlag()
    {
        ActivationCost cost = ActivationCost.Parse("1 Vitae or 1 Willpower");
        Assert.True(cost.IsPlayerChoiceVitaeOrWillpower);
        Assert.Equal(1, cost.Amount);
        Assert.Equal(1, cost.PlayerChoiceWillpowerAmount);
        Assert.Equal(ActivationCostType.Vitae, cost.Type);
    }

    [Fact]
    public void Parse_VitaeOrWillpower_AllowsDistinctAmounts()
    {
        ActivationCost cost = ActivationCost.Parse("2 Vitae or 3 Willpower");
        Assert.True(cost.IsPlayerChoiceVitaeOrWillpower);
        Assert.Equal(2, cost.Amount);
        Assert.Equal(3, cost.PlayerChoiceWillpowerAmount);
    }

    [Theory]
    [InlineData("—")]
    [InlineData("")]
    [InlineData(null)]
    public void Parse_EmptyOrDash_ReturnsNone(string? input)
    {
        ActivationCost cost = ActivationCost.Parse(input);
        Assert.True(cost.IsNone);
    }

    [Fact]
    public void Parse_UnknownType_ReturnsNone()
    {
        ActivationCost cost = ActivationCost.Parse("1 Rouse");
        Assert.True(cost.IsNone);
    }
}
