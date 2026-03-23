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

    /// <summary>Catalog equipment or services (Phase 11 — Assets &amp; Armory).</summary>
    Equipment,
}
