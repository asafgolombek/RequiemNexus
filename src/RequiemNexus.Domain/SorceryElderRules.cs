namespace RequiemNexus.Domain;

/// <summary>
/// Blood Potency threshold for catalog rites whose <c>Ranking</c> is <c>Elder</c> (Theban and other traditions).
/// Aligns with the Phase 19.5 audit default: elder-ranked catalog rites require BP 5+.
/// </summary>
public static class SorceryElderRules
{
    /// <summary>Minimum Blood Potency to learn or cast an elder-ranked ritual from the canonical catalog.</summary>
    public const int MinimumBloodPotency = 5;
}
