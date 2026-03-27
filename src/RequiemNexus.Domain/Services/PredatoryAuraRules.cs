using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Stateless rules for Predatory Aura contests per V:tR 2e pp. 89–90.
/// </summary>
public static class PredatoryAuraRules
{
    /// <summary>
    /// Determines the winner of a contested aura roll.
    /// Ties on successes are broken by higher Blood Potency; if Blood Potency is also tied, the result is a draw.
    /// </summary>
    /// <param name="attackerSuccesses">Total successes on the attacker's Blood Potency pool.</param>
    /// <param name="attackerBp">Attacker's Blood Potency (used only when successes are tied).</param>
    /// <param name="defenderSuccesses">Total successes on the defender's Blood Potency pool.</param>
    /// <param name="defenderBp">Defender's Blood Potency (used only when successes are tied).</param>
    /// <returns>Who won the contest, or <see cref="PredatoryAuraOutcome.Draw"/>.</returns>
    public static PredatoryAuraOutcome ResolveContest(
        int attackerSuccesses,
        int attackerBp,
        int defenderSuccesses,
        int defenderBp)
    {
        if (attackerSuccesses > defenderSuccesses)
        {
            return PredatoryAuraOutcome.AttackerWins;
        }

        if (defenderSuccesses > attackerSuccesses)
        {
            return PredatoryAuraOutcome.DefenderWins;
        }

        if (attackerBp > defenderBp)
        {
            return PredatoryAuraOutcome.AttackerWins;
        }

        if (defenderBp > attackerBp)
        {
            return PredatoryAuraOutcome.DefenderWins;
        }

        return PredatoryAuraOutcome.Draw;
    }
}
