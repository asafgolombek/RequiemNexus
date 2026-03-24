namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Structured reason for spending Vitae on healing (VtR 2e p. 173 patterns).
/// </summary>
public enum HealingReason
{
    /// <summary>Spend Vitae to heal one bashing box quickly in a scene.</summary>
    FastHealBashing,

    /// <summary>Heal lethal damage over time with Vitae (full rules deferred; cost gate only).</summary>
    HealLethal,

    /// <summary>Heal aggravated damage with Vitae (full rules deferred; cost gate only).</summary>
    HealAggravated,
}
