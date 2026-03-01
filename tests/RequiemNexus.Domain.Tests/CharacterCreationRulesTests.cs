using Xunit;
namespace RequiemNexus.Domain.Tests;

public class CharacterCreationRulesTests
{
    // --- CalculateInitialHealth ---

    [Fact]
    public void CalculateInitialHealth_ReturnsCorrectSum()
    {
        var (maxHealth, currentHealth) = CharacterCreationRules.CalculateInitialHealth(size: 5, stamina: 2);

        Assert.Equal(7, maxHealth);
        Assert.Equal(7, currentHealth);
    }

    [Fact]
    public void CalculateInitialHealth_NeonateStartsAtFullHealth()
    {
        var (maxHealth, currentHealth) = CharacterCreationRules.CalculateInitialHealth(size: 5, stamina: 3);

        Assert.Equal(maxHealth, currentHealth);
    }

    [Theory]
    [InlineData(5, 1, 6)]
    [InlineData(5, 5, 10)]
    [InlineData(4, 1, 5)]
    public void CalculateInitialHealth_VariousInputs(int size, int stamina, int expected)
    {
        var (maxHealth, _) = CharacterCreationRules.CalculateInitialHealth(size, stamina);

        Assert.Equal(expected, maxHealth);
    }

    // --- CalculateInitialWillpower ---

    [Fact]
    public void CalculateInitialWillpower_ReturnsCorrectSum()
    {
        var (maxWillpower, currentWillpower) = CharacterCreationRules.CalculateInitialWillpower(resolve: 2, composure: 3);

        Assert.Equal(5, maxWillpower);
        Assert.Equal(5, currentWillpower);
    }

    [Fact]
    public void CalculateInitialWillpower_NeonateStartsAtFullWillpower()
    {
        var (maxWillpower, currentWillpower) = CharacterCreationRules.CalculateInitialWillpower(resolve: 3, composure: 2);

        Assert.Equal(maxWillpower, currentWillpower);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(3, 3, 6)]
    [InlineData(5, 1, 6)]
    public void CalculateInitialWillpower_VariousInputs(int resolve, int composure, int expected)
    {
        var (maxWillpower, _) = CharacterCreationRules.CalculateInitialWillpower(resolve, composure);

        Assert.Equal(expected, maxWillpower);
    }

    // --- CalculateInitialBloodPotencyAndVitae ---

    [Fact]
    public void CalculateInitialBloodPotencyAndVitae_ReturnsStandardNeonateValues()
    {
        var (bloodPotency, maxVitae, currentVitae) = CharacterCreationRules.CalculateInitialBloodPotencyAndVitae();

        Assert.Equal(1, bloodPotency);
        Assert.Equal(10, maxVitae);
        Assert.Equal(10, currentVitae);
    }

    [Fact]
    public void CalculateInitialBloodPotencyAndVitae_NeonateStartsAtFullVitae()
    {
        var (_, maxVitae, currentVitae) = CharacterCreationRules.CalculateInitialBloodPotencyAndVitae();

        Assert.Equal(maxVitae, currentVitae);
    }

    // --- TryConvertBeats ---

    [Fact]
    public void TryConvertBeats_ReturnsFalseWhenBeatsLessThanFive()
    {
        bool converted = CharacterCreationRules.TryConvertBeats(4, out int newBeats, out int xpGained);

        Assert.False(converted);
        Assert.Equal(4, newBeats);
        Assert.Equal(0, xpGained);
    }

    [Fact]
    public void TryConvertBeats_ReturnsFalseWhenBeatsIsZero()
    {
        bool converted = CharacterCreationRules.TryConvertBeats(0, out int newBeats, out int xpGained);

        Assert.False(converted);
        Assert.Equal(0, newBeats);
        Assert.Equal(0, xpGained);
    }

    [Fact]
    public void TryConvertBeats_ReturnsTrueAndConvertsAtExactlyFive()
    {
        bool converted = CharacterCreationRules.TryConvertBeats(5, out int newBeats, out int xpGained);

        Assert.True(converted);
        Assert.Equal(0, newBeats);
        Assert.Equal(1, xpGained);
    }

    [Fact]
    public void TryConvertBeats_RetainsSurplusBeatsAfterConversion()
    {
        // 7 beats → convert 5, keep 2
        bool converted = CharacterCreationRules.TryConvertBeats(7, out int newBeats, out int xpGained);

        Assert.True(converted);
        Assert.Equal(2, newBeats);
        Assert.Equal(1, xpGained);
    }

    [Fact]
    public void TryConvertBeats_OnlyConvertsOncePerCall()
    {
        // Even with 10 beats, a single call only converts 5 → 1 XP
        bool converted = CharacterCreationRules.TryConvertBeats(10, out int newBeats, out int xpGained);

        Assert.True(converted);
        Assert.Equal(5, newBeats); // 5 remain for the next call
        Assert.Equal(1, xpGained);
    }
}
