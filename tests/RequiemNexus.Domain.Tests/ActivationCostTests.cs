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
    public void Parse_VitaeOrWillpower_DefaultsToVitae()
    {
        ActivationCost cost = ActivationCost.Parse("1 Vitae or 1 Willpower");
        Assert.Equal(ActivationCostType.Vitae, cost.Type);
        Assert.Equal(1, cost.Amount);
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
