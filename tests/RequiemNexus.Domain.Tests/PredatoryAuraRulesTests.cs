using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class PredatoryAuraRulesTests
{
    [Fact]
    public void ResolveContest_AttackerMoreSuccesses_WinsRegardlessOfBp()
    {
        PredatoryAuraOutcome o = PredatoryAuraRules.ResolveContest(3, 1, 2, 10);
        Assert.Equal(PredatoryAuraOutcome.AttackerWins, o);
    }

    [Fact]
    public void ResolveContest_DefenderMoreSuccesses_WinsRegardlessOfBp()
    {
        PredatoryAuraOutcome o = PredatoryAuraRules.ResolveContest(1, 10, 2, 1);
        Assert.Equal(PredatoryAuraOutcome.DefenderWins, o);
    }

    [Fact]
    public void ResolveContest_TiedSuccesses_HigherBpAttackerWins()
    {
        PredatoryAuraOutcome o = PredatoryAuraRules.ResolveContest(2, 4, 2, 3);
        Assert.Equal(PredatoryAuraOutcome.AttackerWins, o);
    }

    [Fact]
    public void ResolveContest_TiedSuccesses_HigherBpDefenderWins()
    {
        PredatoryAuraOutcome o = PredatoryAuraRules.ResolveContest(2, 2, 2, 5);
        Assert.Equal(PredatoryAuraOutcome.DefenderWins, o);
    }

    [Fact]
    public void ResolveContest_TrueDraw_EqualSuccessesAndEqualBp()
    {
        PredatoryAuraOutcome o = PredatoryAuraRules.ResolveContest(3, 3, 3, 3);
        Assert.Equal(PredatoryAuraOutcome.Draw, o);
    }
}
