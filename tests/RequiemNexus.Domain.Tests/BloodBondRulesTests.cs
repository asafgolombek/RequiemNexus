using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Unit tests for <see cref="BloodBondRules"/>.
/// </summary>
public class BloodBondRulesTests
{
    [Fact]
    public void IsFading_IsTrue_WhenNeverFed()
    {
        DateTime now = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(BloodBondRules.IsFading(null, now));
    }

    [Fact]
    public void IsFading_IsFalse_OnExactThresholdBoundary()
    {
        DateTime now = new(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime fed = now - BloodBondRules.FadingThreshold;
        Assert.False(BloodBondRules.IsFading(fed, now));
    }

    [Fact]
    public void IsFading_IsTrue_WhenOneSecondPastThreshold()
    {
        DateTime now = new(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime fed = now - BloodBondRules.FadingThreshold - TimeSpan.FromSeconds(1);
        Assert.True(BloodBondRules.IsFading(fed, now));
    }

    [Theory]
    [InlineData(1, ConditionType.Addicted)]
    [InlineData(2, ConditionType.Swooned)]
    [InlineData(3, ConditionType.Bound)]
    public void ConditionForStage_ReturnsExpectedType(int stage, ConditionType expected)
    {
        Assert.Equal(expected, BloodBondRules.ConditionForStage(stage));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void ConditionForStage_ThrowsForInvalidStage(int stage)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BloodBondRules.ConditionForStage(stage));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void StageResolutionAwardsBeat_OnlyForStageThree(int stage, bool expected)
    {
        Assert.Equal(expected, BloodBondRules.StageResolutionAwardsBeat(stage));
    }
}
