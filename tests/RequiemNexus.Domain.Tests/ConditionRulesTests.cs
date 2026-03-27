using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Pure unit tests for <see cref="ConditionRules"/> — no database required.
/// </summary>
public class ConditionRulesTests
{
    private readonly ConditionRules _rules = new();

    // -----------------------------------------------------------------------
    // AwardsBeatOnResolve
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(ConditionType.Guilty)]
    [InlineData(ConditionType.Swooned)]
    [InlineData(ConditionType.Tempted)]
    [InlineData(ConditionType.Shaken)]
    [InlineData(ConditionType.Notoriety)]
    [InlineData(ConditionType.Leveraged)]
    [InlineData(ConditionType.Exhausted)]
    [InlineData(ConditionType.Despondent)]
    [InlineData(ConditionType.Frightened)]
    [InlineData(ConditionType.Bleeding)]
    [InlineData(ConditionType.OnFire)]
    [InlineData(ConditionType.Immolating)]
    [InlineData(ConditionType.Wassail)]
    [InlineData(ConditionType.Provoked)]
    [InlineData(ConditionType.Inspired)]
    [InlineData(ConditionType.Bound)]
    public void AwardsBeatOnResolve_ReturnsTrue_ForCanonicalConditions(ConditionType type)
    {
        Assert.True(_rules.AwardsBeatOnResolve(type));
    }

    [Fact]
    public void AwardsBeatOnResolve_ReturnsFalse_ForCustomCondition()
    {
        Assert.False(_rules.AwardsBeatOnResolve(ConditionType.Custom));
    }

    [Fact]
    public void AwardsBeatOnResolve_ReturnsFalse_ForAddictedCondition()
    {
        Assert.False(_rules.AwardsBeatOnResolve(ConditionType.Addicted));
    }

    // -----------------------------------------------------------------------
    // GetConditionDescription
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(ConditionType.Guilty)]
    [InlineData(ConditionType.Swooned)]
    [InlineData(ConditionType.Shaken)]
    [InlineData(ConditionType.Custom)]
    [InlineData(ConditionType.Addicted)]
    [InlineData(ConditionType.Bound)]
    public void GetConditionDescription_ReturnsNonEmptyString(ConditionType type)
    {
        string desc = _rules.GetConditionDescription(type);
        Assert.NotEmpty(desc);
    }

    // -----------------------------------------------------------------------
    // GetTiltDescription
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(TiltType.KnockedDown)]
    [InlineData(TiltType.Stunned)]
    [InlineData(TiltType.Blinded)]
    [InlineData(TiltType.Frenzy)]
    [InlineData(TiltType.Custom)]
    [InlineData(TiltType.BeatenDown)]
    public void GetTiltDescription_ReturnsNonEmptyString(TiltType type)
    {
        string desc = _rules.GetTiltDescription(type);
        Assert.NotEmpty(desc);
    }

    // -----------------------------------------------------------------------
    // GetTiltEffects
    // -----------------------------------------------------------------------

    [Fact]
    public void GetTiltEffects_ReturnsEmpty_WhenNoTilts()
    {
        IReadOnlyList<string> effects = _rules.GetTiltEffects([]);
        Assert.Empty(effects);
    }

    [Fact]
    public void GetTiltEffects_ReturnsPenalty_ForKnockedDown()
    {
        IReadOnlyList<string> effects = _rules.GetTiltEffects([TiltType.KnockedDown]);
        Assert.Single(effects);
        Assert.Contains("−2", effects[0]);
    }

    [Fact]
    public void GetTiltEffects_ReturnsMultiplePenalties_ForMultipleTilts()
    {
        IReadOnlyList<string> effects = _rules.GetTiltEffects(
            [TiltType.Blinded, TiltType.LegWrack]);

        Assert.Equal(2, effects.Count);
    }

    [Fact]
    public void GetTiltEffects_IgnoresCustomTilt_WithoutCrashing()
    {
        // Custom tilts have no mechanical effect catalog entry — should not throw
        IReadOnlyList<string> effects = _rules.GetTiltEffects([TiltType.Custom]);
        Assert.Empty(effects);
    }

    [Fact]
    public void GetTiltEffects_ReturnsPenalty_ForBeatenDown()
    {
        IReadOnlyList<string> effects = _rules.GetTiltEffects([TiltType.BeatenDown]);
        Assert.Single(effects);
        Assert.Contains("−2", effects[0]);
    }
}
