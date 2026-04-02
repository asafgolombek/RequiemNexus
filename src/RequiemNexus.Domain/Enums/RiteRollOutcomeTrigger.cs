namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Maps V:tR 2e blood sorcery extended-ritual outcomes to Conditions (Phase 19.5 P1-3).
/// </summary>
public enum RiteRollOutcomeTrigger
{
    /// <summary>Dramatic failure on a ritual roll — tradition-specific Condition (Crúac / Theban only).</summary>
    DramaticFailure,

    /// <summary>Exceptional success on a ritual roll — tradition-specific Condition (Crúac / Theban only).</summary>
    ExceptionalSuccess,

    /// <summary>Player chose to continue the rite after a roll with zero successes — Stumbled (all traditions).</summary>
    ContinueAfterZeroSuccesses,
}
