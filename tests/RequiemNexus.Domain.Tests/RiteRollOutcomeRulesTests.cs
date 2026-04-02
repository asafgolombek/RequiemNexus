using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class RiteRollOutcomeRulesTests
{
    [Theory]
    [InlineData(SorceryType.Cruac, RiteRollOutcomeTrigger.DramaticFailure, ConditionType.Tempted)]
    [InlineData(SorceryType.Theban, RiteRollOutcomeTrigger.DramaticFailure, ConditionType.Humbled)]
    [InlineData(SorceryType.Cruac, RiteRollOutcomeTrigger.ExceptionalSuccess, ConditionType.Ecstatic)]
    [InlineData(SorceryType.Theban, RiteRollOutcomeTrigger.ExceptionalSuccess, ConditionType.Raptured)]
    [InlineData(SorceryType.Necromancy, RiteRollOutcomeTrigger.ContinueAfterZeroSuccesses, ConditionType.Stumbled)]
    [InlineData(SorceryType.Cruac, RiteRollOutcomeTrigger.ContinueAfterZeroSuccesses, ConditionType.Stumbled)]
    public void TryResolveConditionType_ReturnsExpected(SorceryType tradition, RiteRollOutcomeTrigger trigger, ConditionType expected)
    {
        ConditionType? actual = RiteRollOutcomeRules.TryResolveConditionType(tradition, trigger);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(SorceryType.Necromancy, RiteRollOutcomeTrigger.DramaticFailure)]
    [InlineData(SorceryType.Necromancy, RiteRollOutcomeTrigger.ExceptionalSuccess)]
    public void TryResolveConditionType_ReturnsNull_WhenNoTraditionSpecificOutcome(SorceryType tradition, RiteRollOutcomeTrigger trigger)
    {
        Assert.Null(RiteRollOutcomeRules.TryResolveConditionType(tradition, trigger));
    }
}
