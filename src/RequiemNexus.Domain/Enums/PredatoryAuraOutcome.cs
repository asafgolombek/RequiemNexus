namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Result of a contested Predatory Aura (Blood Potency) roll between two Kindred.
/// </summary>
public enum PredatoryAuraOutcome
{
    /// <summary>The attacker rolled more successes, or tied successes with higher Blood Potency.</summary>
    AttackerWins,

    /// <summary>The defender rolled more successes, or tied successes with higher Blood Potency.</summary>
    DefenderWins,

    /// <summary>Equal successes and equal Blood Potency — no mechanical effect.</summary>
    Draw,
}
