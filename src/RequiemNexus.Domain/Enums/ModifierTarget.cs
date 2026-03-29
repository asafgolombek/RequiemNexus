namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Strongly-typed targets for passive modifiers. Used instead of freeform strings (Antigravity Rule #1).
/// </summary>
public enum ModifierTarget
{
    Defense,

    Speed,

    MaxHealth,

    Brawl,

    WoundPenalty,

    Athletics,

    Weaponry,

    Firearms,

    FirearmsAccuracy,

    /// <summary>
    /// Equipment or service bonus tied to a specific skill; see <c>PassiveModifier.AppliesToSkill</c>.
    /// </summary>
    SkillPool,

    /// <summary>Condition: penalty applies to every dice pool resolved by the trait/pool resolver.</summary>
    AllDicePools,

    /// <summary>Condition: penalty when the pool includes any Physical skill.</summary>
    PhysicalDicePools,

    /// <summary>Condition: penalty when the pool includes any Mental skill.</summary>
    MentalDicePools,

    /// <summary>Condition: Frightened — same application as <see cref="AllDicePools"/> until flee-intent pool metadata exists.</summary>
    DicePoolsExceptFleeing,

    /// <summary>Condition: penalty when the pool includes Resolve or Composure attributes.</summary>
    PoolsUsingResolveOrComposure,

    /// <summary>Condition: penalty when the pool includes the Composure attribute.</summary>
    PoolsUsingComposureAttribute,
}
