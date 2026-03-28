using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class HuntResonanceMapperTests
{
    [Theory]
    [InlineData(0, ResonanceOutcome.None)]
    [InlineData(1, ResonanceOutcome.Fleeting)]
    [InlineData(2, ResonanceOutcome.Fleeting)]
    [InlineData(3, ResonanceOutcome.Weak)]
    [InlineData(4, ResonanceOutcome.Weak)]
    [InlineData(5, ResonanceOutcome.Functional)]
    [InlineData(6, ResonanceOutcome.Functional)]
    [InlineData(7, ResonanceOutcome.Saturated)]
    [InlineData(12, ResonanceOutcome.Saturated)]
    public void FromSuccesses_MatchesPhase16aTable(int successes, ResonanceOutcome expected)
    {
        Assert.Equal(expected, HuntResonanceMapper.FromSuccesses(successes));
    }
}
