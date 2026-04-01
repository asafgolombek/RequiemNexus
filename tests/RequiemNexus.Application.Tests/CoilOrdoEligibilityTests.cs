using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Unit tests for <see cref="CoilOrdoEligibility"/> pure helpers.
/// </summary>
public class CoilOrdoEligibilityTests
{
    [Theory]
    [InlineData(true, true, 2)]
    [InlineData(true, false, 3)]
    [InlineData(false, true, 3)]
    [InlineData(false, false, 4)]
    public void CalculateXpCost_ReturnsExpectedMatrix(bool chosenMystery, bool crucible, int expected)
    {
        Assert.Equal(expected, CoilOrdoEligibility.CalculateXpCost(chosenMystery, crucible));
    }

    [Fact]
    public void IsOrdoDraculMember_FalseWhenCovenantPending()
    {
        var character = new Character
        {
            Covenant = new CovenantDefinition { Name = CoilOrdoEligibility.OrdoDraculName },
            CovenantJoinStatus = CovenantJoinStatus.Pending,
        };

        Assert.False(CoilOrdoEligibility.IsOrdoDraculMember(character));
    }

    [Fact]
    public void IsOrdoDraculMember_TrueWhenAlignedOrdoAndNotPending()
    {
        var character = new Character
        {
            Covenant = new CovenantDefinition { Name = CoilOrdoEligibility.OrdoDraculName },
            CovenantJoinStatus = null,
        };

        Assert.True(CoilOrdoEligibility.IsOrdoDraculMember(character));
    }
}
