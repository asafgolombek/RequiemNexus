namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Type of passive modifier affecting how it applies to derived stats.
/// </summary>
public enum ModifierType
{
    /// <summary>Permanent bonus/penalty (e.g., +1 Defense from a Coil).</summary>
    Static,

    /// <summary>Applies only under specific circumstances (e.g., +2 when resisting frenzy).</summary>
    Conditional,

    /// <summary>Alters engine behavior via explicit flags, not a numeric delta (e.g., ignore wound penalties).</summary>
    RuleBreaking,
}
