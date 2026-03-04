using Xunit;
namespace RequiemNexus.Domain.Tests;

public class ExperienceCostRulesTests
{
    [Theory]
    [InlineData(1, 2, 8)]   // 2 * 4 = 8
    [InlineData(1, 3, 20)]  // (2*4) + (3*4) = 8 + 12 = 20
    [InlineData(2, 4, 28)]  // (3*4) + (4*4) = 12 + 16 = 28
    public void CalculateAttributeUpgradeCost_ReturnsCorrectValue(int from, int to, int expected)
    {
        int cost = ExperienceCostRules.CalculateAttributeUpgradeCost(from, to);
        Assert.Equal(expected, cost);
    }

    [Theory]
    [InlineData(0, 1, 2)]   // 1 * 2 = 2
    [InlineData(1, 3, 10)]  // (2*2) + (3*2) = 4 + 6 = 10
    [InlineData(0, 5, 30)]  // (1+2+3+4+5)*2 = 15*2 = 30
    public void CalculateSkillUpgradeCost_ReturnsCorrectValue(int from, int to, int expected)
    {
        int cost = ExperienceCostRules.CalculateSkillUpgradeCost(from, to);
        Assert.Equal(expected, cost);
    }

    [Theory]
    [InlineData(1, 2, 10)]  // 2 * 5 = 10
    [InlineData(0, 3, 30)]  // (1+2+3)*5 = 30
    public void CalculateDisciplineUpgradeCost_ReturnsCorrectValue(int from, int to, int expected)
    {
        int cost = ExperienceCostRules.CalculateDisciplineUpgradeCost(from, to);
        Assert.Equal(expected, cost);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    public void CalculateMeritCost_ReturnsRating(int rating, int expected)
    {
        int cost = ExperienceCostRules.CalculateMeritCost(rating);
        Assert.Equal(expected, cost);
    }

    [Fact]
    public void CalculateUpgradeCost_ReturnsZeroWhenNoUpgrade()
    {
        Assert.Equal(0, ExperienceCostRules.CalculateUpgradeCost(3, 3, 4));
        Assert.Equal(0, ExperienceCostRules.CalculateUpgradeCost(3, 2, 4));
    }
}
