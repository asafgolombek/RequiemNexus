using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class ExperienceCostRulesTests
{
    private readonly ExperienceCostRules _rules = new();

    [Theory]
    [InlineData(1, 2, 8)]   // 2 * 4 = 8
    [InlineData(1, 3, 20)]  // (2*4) + (3*4) = 8 + 12 = 20
    [InlineData(2, 4, 28)]  // (3*4) + (4*4) = 12 + 16 = 28
    public void CalculateAttributeUpgradeCost_ReturnsCorrectValue(int from, int to, int expected)
    {
        int cost = _rules.CalculateAttributeUpgradeCost(from, to);
        Assert.Equal(expected, cost);
    }

    [Theory]
    [InlineData(0, 1, 2)]   // 1 * 2 = 2
    [InlineData(1, 3, 10)]  // (2*2) + (3*2) = 4 + 6 = 10
    [InlineData(0, 5, 30)]  // (1+2+3+4+5)*2 = 15*2 = 30
    public void CalculateSkillUpgradeCost_ReturnsCorrectValue(int from, int to, int expected)
    {
        int cost = _rules.CalculateSkillUpgradeCost(from, to);
        Assert.Equal(expected, cost);
    }

    [Theory]
    [InlineData(1, 2, 10, false)]   // 2 * 5 = 10 (out-of-clan)
    [InlineData(0, 3, 30, false)]   // (1+2+3)*5 = 30 (out-of-clan)
    [InlineData(1, 2, 8, true)]    // 2 * 4 = 8 (in-clan)
    [InlineData(0, 3, 24, true)]   // (1+2+3)*4 = 24 (in-clan)
    public void CalculateDisciplineUpgradeCost_ReturnsCorrectValue(int from, int to, int expected, bool isInClan)
    {
        int cost = _rules.CalculateDisciplineUpgradeCost(from, to, isInClan);
        Assert.Equal(expected, cost);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 2, 1)]  // 1 XP per dot flat
    [InlineData(0, 5, 5)]
    public void CalculateMeritCost_ReturnsCorrectCost(int from, int to, int expected)
    {
        int cost = _rules.CalculateMeritCost(from, to);
        Assert.Equal(expected, cost);
    }

    [Fact]
    public void CalculateUpgradeCost_ReturnsZeroWhenNoUpgrade()
    {
        Assert.Equal(0, ExperienceCostRules.CalculateUpgradeCost(3, 3, 4));
        Assert.Equal(0, ExperienceCostRules.CalculateUpgradeCost(3, 2, 4));
    }
}

