using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class BloodSympathyRulesTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(5, 2)]
    [InlineData(6, 3)]
    public void ComputeRating_MatchesBloodPotencyHalvedWithMinimum(int bloodPotency, int expected)
    {
        Assert.Equal(expected, BloodSympathyRules.ComputeRating(bloodPotency));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(2, 3, 2)]
    [InlineData(5, 1, 1)]
    public void EffectiveRange_IsMinimumOfBothRatings(int a, int b, int expected)
    {
        Assert.Equal(expected, BloodSympathyRules.EffectiveRange(a, b));
    }

    [Theory]
    [InlineData(4, 1, 4)]
    [InlineData(4, 2, 2)]
    [InlineData(4, 3, 1)]
    [InlineData(4, 5, 0)]
    [InlineData(3, 0, 0)]
    [InlineData(3, -1, 0)]
    public void BonusDiceForDegree_IsRatingOverDegreeFloored(int rating, int degree, int expected)
    {
        Assert.Equal(expected, BloodSympathyRules.BonusDiceForDegree(rating, degree));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 3)]
    [InlineData(2, 2)]
    [InlineData(3, 1)]
    [InlineData(4, 0)]
    public void RitualSympathyBonusThebanOrNecromancy_MatchesTable(int degree, int expected)
    {
        Assert.Equal(expected, BloodSympathyRules.RitualSympathyBonusThebanOrNecromancy(degree));
    }
}
