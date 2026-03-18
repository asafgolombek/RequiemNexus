using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Tests for the Passive Modifier Engine (Static, Conditional, RuleBreaking).
/// </summary>
public class ModifierEngineTests
{
    [Fact]
    public void PassiveModifier_Static_HasValueAndTarget()
    {
        var source = new ModifierSource(ModifierSourceType.Coil, 1);
        var modifier = new PassiveModifier(ModifierTarget.Defense, 1, ModifierType.Static, null, source);

        Assert.Equal(ModifierTarget.Defense, modifier.Target);
        Assert.Equal(1, modifier.Value);
        Assert.Equal(ModifierType.Static, modifier.ModifierType);
        Assert.Equal(1, modifier.Source.SourceId);
    }

    [Fact]
    public void PassiveModifier_Conditional_HasCondition()
    {
        var source = new ModifierSource(ModifierSourceType.Devotion, 5);
        var modifier = new PassiveModifier(
            ModifierTarget.WoundPenalty,
            -1,
            ModifierType.Conditional,
            "when resisting frenzy",
            source);

        Assert.Equal("when resisting frenzy", modifier.Condition);
        Assert.Equal(ModifierType.Conditional, modifier.ModifierType);
    }

    [Fact]
    public void PassiveModifier_RuleBreaking_HasSourceForDebuggability()
    {
        var source = new ModifierSource(ModifierSourceType.Coil, 3);
        var modifier = new PassiveModifier(
            ModifierTarget.MaxHealth,
            0,
            ModifierType.RuleBreaking,
            "Ignore first 2 sunlight damage",
            source);

        Assert.Equal(ModifierType.RuleBreaking, modifier.ModifierType);
        Assert.Equal(ModifierSourceType.Coil, modifier.Source.SourceType);
    }

    [Fact]
    public void ModifierSource_TracksSourceTypeAndId()
    {
        var source = new ModifierSource(ModifierSourceType.CovenantBenefit, 42);

        Assert.Equal(ModifierSourceType.CovenantBenefit, source.SourceType);
        Assert.Equal(42, source.SourceId);
    }
}
