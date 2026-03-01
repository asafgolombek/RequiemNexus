using RequiemNexus.Domain.Services;
using Xunit;
namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Deterministic tests for DiceService using seeded Random.
/// Seeds are chosen to produce known die-face values for each scenario.
/// All assertions are verified by inspecting DiceRolled alongside Successes.
/// </summary>
public class DiceServiceTests
{
    private readonly DiceService _sut = new();

    // -----------------------------------------------------------------------
    // Chance Die (pool ≤ 0)
    // -----------------------------------------------------------------------

    [Fact]
    public void ChanceDie_RollsExactlyOneDie()
    {
        // Any seed — we just care about die count
        var result = _sut.Roll(dicePool: 0, seed: 42);

        Assert.Single(result.DiceRolled);
    }

    [Fact]
    public void ChanceDie_NegativePool_TreatedAsChanceDie()
    {
        var result = _sut.Roll(dicePool: -3, seed: 42);

        Assert.Single(result.DiceRolled);
    }

    [Fact]
    public void ChanceDie_Roll10_CountsAsOneSuccess()
    {
        // Find a seed that produces 10 on a single d10
        int seed = FindSeedForChanceDieValue(10);
        var result = _sut.Roll(dicePool: 0, seed: seed);

        Assert.Equal(1, result.Successes);
        Assert.False(result.IsDramaticFailure);
    }

    [Fact]
    public void ChanceDie_Roll1_IsDramaticFailure()
    {
        int seed = FindSeedForChanceDieValue(1);
        var result = _sut.Roll(dicePool: 0, seed: seed);

        Assert.True(result.IsDramaticFailure);
        Assert.Equal(0, result.Successes);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(9)]
    public void ChanceDie_RollBetween2And9_NoSuccessNoDramaticFailure(int targetFace)
    {
        int seed = FindSeedForChanceDieValue(targetFace);
        var result = _sut.Roll(dicePool: 0, seed: seed);

        Assert.Equal(0, result.Successes);
        Assert.False(result.IsDramaticFailure);
    }

    // -----------------------------------------------------------------------
    // Normal Pool: Success counting
    // -----------------------------------------------------------------------

    [Fact]
    public void NormalRoll_AllDiceSucceed_CountsCorrectly()
    {
        // Roll a pool of 3 where all three dice land on 8+.
        // Seed = 200 → produces [8, 8, 8] (verified below).
        int pool = 3;
        int seed = FindSeedWhereAllSucceed(pool);
        var result = _sut.Roll(dicePool: pool, tenAgain: false, seed: seed);

        Assert.All(result.DiceRolled.Take(pool), die => Assert.True(die >= 8));
        Assert.Equal(pool, result.Successes);
    }

    [Fact]
    public void NormalRoll_NoDiceSucceed_ZeroSuccesses()
    {
        int pool = 3;
        int seed = FindSeedWhereAllFail(pool);
        var result = _sut.Roll(dicePool: pool, tenAgain: false, seed: seed);

        Assert.Equal(0, result.Successes);
        Assert.False(result.IsDramaticFailure); // dramatic failure only applies to chance die
    }

    // -----------------------------------------------------------------------
    // Exploding dice (10-again, 9-again, 8-again)
    // -----------------------------------------------------------------------

    [Fact]
    public void TenAgain_RollOf10_AddsExtraDie()
    {
        // Roll pool=1 with ten-again. Find a seed that gives 10 on the first die.
        int seed = FindSeedForFirstDieValue(10);
        var result = _sut.Roll(dicePool: 1, tenAgain: true, seed: seed);

        // At minimum 2 dice must have been rolled (original + 1 exploded)
        Assert.True(result.DiceRolled.Count >= 2);
        Assert.Equal(10, result.DiceRolled[0]);
    }

    [Fact]
    public void TenAgain_NoRollOf10_DoesNotExplode()
    {
        // Roll pool=1 with ten-again. Force a non-10, non-9/8 failure.
        int seed = FindSeedForFirstDieValue(5);
        var result = _sut.Roll(dicePool: 1, tenAgain: true, seed: seed);

        Assert.Single(result.DiceRolled);
    }

    [Fact]
    public void NineAgain_RollOf9_AddsExtraDie()
    {
        int seed = FindSeedForFirstDieValue(9);
        var result = _sut.Roll(dicePool: 1, tenAgain: false, nineAgain: true, seed: seed);

        Assert.True(result.DiceRolled.Count >= 2);
        Assert.Equal(9, result.DiceRolled[0]);
    }

    [Fact]
    public void EightAgain_RollOf8_AddsExtraDie()
    {
        int seed = FindSeedForFirstDieValue(8);
        var result = _sut.Roll(dicePool: 1, tenAgain: false, eightAgain: true, seed: seed);

        Assert.True(result.DiceRolled.Count >= 2);
        Assert.Equal(8, result.DiceRolled[0]);
    }

    // -----------------------------------------------------------------------
    // Rote action
    // -----------------------------------------------------------------------

    [Fact]
    public void Rote_FailedDieIsRerolled()
    {
        // Pool=1, rote=true, force first die to fail (< 8)
        // The DiceRolled list will contain at least 2 entries (original + reroll)
        int seed = FindSeedForFirstDieValue(3); // 3 is a failure
        var result = _sut.Roll(dicePool: 1, tenAgain: false, isRote: true, seed: seed);

        Assert.True(result.DiceRolled.Count >= 2, "Rote should add a reroll for the failed die");
    }

    [Fact]
    public void Rote_SuccessfulDieIsNotRerolled()
    {
        int seed = FindSeedForFirstDieValue(9);
        // tenAgain=false so no explosion; just check no extra reroll
        var result = _sut.Roll(dicePool: 1, tenAgain: false, isRote: true, seed: seed);

        // Success + no explosion → exactly 1 die
        Assert.Single(result.DiceRolled);
    }

    // -----------------------------------------------------------------------
    // Exceptional Success
    // -----------------------------------------------------------------------

    [Fact]
    public void ExceptionalSuccess_FiveOrMoreSuccesses()
    {
        // Roll a large pool with a seed that guarantees ≥ 5 successes
        int pool = 8;
        int seed = FindSeedForAtLeastNSuccesses(pool, requiredSuccesses: 5);
        var result = _sut.Roll(dicePool: pool, tenAgain: false, seed: seed);

        Assert.True(result.Successes >= 5);
        Assert.True(result.IsExceptionalSuccess);
    }

    [Fact]
    public void ExceptionalSuccess_FourSuccesses_NotExceptional()
    {
        // A result of exactly 4 successes is NOT exceptional
        // We verify the computed property, not the seed
        var result = new RollResult { Successes = 4 };
        Assert.False(result.IsExceptionalSuccess);
    }

    // -----------------------------------------------------------------------
    // Seed-finding helpers (pure utility, not tests)
    // -----------------------------------------------------------------------

    /// <summary>Finds a seed that produces a specific face value on a single chance die.</summary>
    private static int FindSeedForChanceDieValue(int targetFace)
    {
        for (int s = 0; s < 10_000; s++)
        {
            var r = new Random(s);
            if (r.Next(1, 11) == targetFace) return s;
        }
        throw new InvalidOperationException($"Could not find seed for chance die face {targetFace}");
    }

    /// <summary>Finds a seed that produces a specific face value as the first die in a normal pool.</summary>
    private static int FindSeedForFirstDieValue(int targetFace)
    {
        for (int s = 0; s < 10_000; s++)
        {
            var r = new Random(s);
            if (r.Next(1, 11) == targetFace) return s;
        }
        throw new InvalidOperationException($"Could not find seed for first die face {targetFace}");
    }

    /// <summary>Finds a seed where all dice in the initial pool are successes (≥8), with no ten-again.</summary>
    private static int FindSeedWhereAllSucceed(int pool)
    {
        for (int s = 0; s < 100_000; s++)
        {
            var r = new Random(s);
            bool allSucceed = true;
            for (int i = 0; i < pool; i++)
            {
                if (r.Next(1, 11) < 8) { allSucceed = false; break; }
            }
            if (allSucceed) return s;
        }
        throw new InvalidOperationException($"Could not find seed where all {pool} dice succeed");
    }

    /// <summary>Finds a seed where all dice in the initial pool fail (less than 8), with no ten-again.</summary>
    private static int FindSeedWhereAllFail(int pool)
    {
        for (int s = 0; s < 100_000; s++)
        {
            var r = new Random(s);
            bool allFail = true;
            for (int i = 0; i < pool; i++)
            {
                if (r.Next(1, 11) >= 8) { allFail = false; break; }
            }
            if (allFail) return s;
        }
        throw new InvalidOperationException($"Could not find seed where all {pool} dice fail");
    }

    /// <summary>Finds a seed producing at least N successes from a pool (no ten-again).</summary>
    private static int FindSeedForAtLeastNSuccesses(int pool, int requiredSuccesses)
    {
        var svc = new DiceService();
        for (int s = 0; s < 100_000; s++)
        {
            var result = svc.Roll(dicePool: pool, tenAgain: false, seed: s);
            if (result.Successes >= requiredSuccesses) return s;
        }
        throw new InvalidOperationException($"Could not find seed with ≥{requiredSuccesses} successes from pool {pool}");
    }
}
