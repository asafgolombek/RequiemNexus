using RequiemNexus.Domain;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Unit tests for <see cref="GhoulAgingRules"/>.
/// </summary>
public class GhoulAgingRulesTests
{
    [Fact]
    public void IsAgingDue_IsTrue_WhenNeverFed()
    {
        DateTime now = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.True(GhoulAgingRules.IsAgingDue(null, now));
    }

    [Fact]
    public void IsAgingDue_IsFalse_OnExactThresholdBoundary()
    {
        DateTime now = new(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime fed = now - GhoulAgingRules.FeedingInterval;
        Assert.False(GhoulAgingRules.IsAgingDue(fed, now));
    }

    [Fact]
    public void IsAgingDue_IsTrue_WhenOneMinutePastThreshold()
    {
        DateTime now = new(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime fed = now - GhoulAgingRules.FeedingInterval - TimeSpan.FromMinutes(1);
        Assert.True(GhoulAgingRules.IsAgingDue(fed, now));
    }

    [Fact]
    public void OverdueMonths_IsZero_WithinGracePeriod()
    {
        DateTime now = new(2025, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime fed = now - GhoulAgingRules.FeedingInterval;
        Assert.Equal(0, GhoulAgingRules.OverdueMonths(fed, now));
    }

    [Fact]
    public void OverdueMonths_IsOne_AfterSixtyDaysSinceLastFed()
    {
        DateTime fed = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime now = fed + TimeSpan.FromDays(60);
        Assert.Equal(1, GhoulAgingRules.OverdueMonths(fed, now));
    }

    [Fact]
    public void OverdueMonths_IsThree_AfterOneHundredTwentyDaysSinceLastFed()
    {
        DateTime fed = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime now = fed + TimeSpan.FromDays(120);
        Assert.Equal(3, GhoulAgingRules.OverdueMonths(fed, now));
    }
}
