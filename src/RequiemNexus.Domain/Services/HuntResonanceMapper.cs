using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Maps hunt success counts to display resonance (Phase 16a — no mechanical effect).
/// </summary>
public static class HuntResonanceMapper
{
    /// <summary>
    /// Thresholds: 0 → None; 1–2 Fleeting; 3–4 Weak; 5–6 Functional; 7+ Saturated.
    /// </summary>
    public static ResonanceOutcome FromSuccesses(int successes) => successes switch
    {
        0 => ResonanceOutcome.None,
        <= 2 => ResonanceOutcome.Fleeting,
        <= 4 => ResonanceOutcome.Weak,
        <= 6 => ResonanceOutcome.Functional,
        _ => ResonanceOutcome.Saturated,
    };
}
