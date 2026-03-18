namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Type of entity that generated a passive modifier. Required for debuggability (Antigravity Rule #8).
/// </summary>
public enum ModifierSourceType
{
    Coil,

    Devotion,

    CovenantBenefit,

    Bloodline,

    Merit,

    Condition,
}
