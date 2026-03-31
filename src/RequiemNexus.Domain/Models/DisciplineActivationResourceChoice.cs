namespace RequiemNexus.Domain.Models;

/// <summary>
/// Which resource the player spends when a Discipline power costs Vitae <strong>or</strong> Willpower.
/// </summary>
public enum DisciplineActivationResourceChoice
{
    /// <summary>Spend Vitae (see <see cref="ActivationCost.Amount"/>).</summary>
    Vitae,

    /// <summary>Spend Willpower (see <see cref="ActivationCost.PlayerChoiceWillpowerAmount"/>).</summary>
    Willpower,
}
