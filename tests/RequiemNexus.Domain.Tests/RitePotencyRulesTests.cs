using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public sealed class RitePotencyRulesTests
{
    [Theory]
    [InlineData(0, 6, 0)]
    [InlineData(5, 6, 0)]
    [InlineData(6, 6, 1)]
    [InlineData(8, 6, 3)]
    [InlineData(13, 13, 1)]
    public void ComputeBasePotency_MatchesVtR2eSidebar(int accumulated, int target, int expected) =>
        Assert.Equal(expected, RitePotencyRules.ComputeBasePotency(accumulated, target));

    [Fact]
    public void ComputeBasePotency_NonPositiveTarget_ReturnsZero() =>
        Assert.Equal(0, RitePotencyRules.ComputeBasePotency(10, 0));
}
