namespace RequiemNexus.Domain.Enums;

/// <summary>Resonance intensity of Vitae gained from a hunt. Display-only in Phase 16a.</summary>
public enum ResonanceOutcome
{
    /// <summary>No successes — no resonance.</summary>
    None = 0,

    /// <summary>1–2 successes.</summary>
    Fleeting = 1,

    /// <summary>3–4 successes.</summary>
    Weak = 2,

    /// <summary>5–6 successes.</summary>
    Functional = 3,

    /// <summary>7+ successes.</summary>
    Saturated = 4,
}
