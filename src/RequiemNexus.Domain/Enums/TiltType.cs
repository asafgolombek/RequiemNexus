namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Canonical Tilts from Vampire: The Requiem 2nd Edition.
/// Tilts are transient combat states — they last for a scene or until the triggering condition ends.
/// Unlike Conditions, Tilts do not award a Beat when removed.
/// </summary>
public enum TiltType
{
    // ── Environmental ────────────────────────────────────────────────────────

    /// <summary>Dim or absent lighting. −2 to vision-based rolls.</summary>
    DimLight,

    /// <summary>Target is obscured by smoke, fog, or similar. −2 to ranged attacks.</summary>
    Concealment,

    /// <summary>Ground is slippery or unstable. −1 to all physical actions.</summary>
    UnstableGround,

    // ── Physical ─────────────────────────────────────────────────────────────

    /// <summary>Character has been knocked to the ground. Suffers −2 to Defense until they spend an action to stand.</summary>
    KnockedDown,

    /// <summary>Character is temporarily dazed. Cannot take an action this turn.</summary>
    Stunned,

    /// <summary>Character cannot see. All attack and Perception rolls suffer −3.</summary>
    Blinded,

    /// <summary>One arm is incapacitated. Cannot perform two-handed actions.</summary>
    ArmWrack,

    /// <summary>One leg is incapacitated. Speed reduced to 1; cannot Dodge.</summary>
    LegWrack,

    /// <summary>Character is pinned and cannot move. Attacks against them gain +2.</summary>
    Immobilized,

    // ── Vampire-Specific ─────────────────────────────────────────────────────

    /// <summary>Character is in frenzy. Controlled by the Beast; cannot use Disciplines that require focus.</summary>
    Frenzy,

    /// <summary>Character is paralysed by Rotschreck (fire/sunlight fear). Must flee or cower.</summary>
    Rotschreck,

    // ── Custom ───────────────────────────────────────────────────────────────

    /// <summary>A Storyteller-defined tilt not covered by the canonical list.</summary>
    Custom,
}
