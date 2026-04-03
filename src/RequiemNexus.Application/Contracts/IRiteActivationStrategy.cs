using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Tradition-specific rules for blood sorcery rite activation orchestrated by <see cref="ISorceryActivationService"/>.
/// </summary>
public interface IRiteActivationStrategy
{
    /// <summary>
    /// Gets the sorcery tradition this strategy handles.
    /// </summary>
    SorceryType Tradition { get; }

    /// <summary>
    /// Gets discipline dots for this tradition (rite level eligibility and extended-action interval).
    /// </summary>
    /// <param name="character">The ritualist.</param>
    /// <returns>Current dots in the tradition discipline.</returns>
    int GetTraditionDisciplineDots(Character character);

    /// <summary>
    /// Validates tradition-specific prerequisites before Vitae and Willpower costs are applied.
    /// </summary>
    /// <param name="character">The ritualist.</param>
    /// <param name="def">The rite definition being activated.</param>
    /// <exception cref="InvalidOperationException">When tradition rules block activation.</exception>
    void ValidateTraditionRules(Character character, SorceryRiteDefinition def);

    /// <summary>
    /// Resolves optional extra Vitae spend for pool bonus dice, or throws if the request is invalid for this tradition.
    /// </summary>
    /// <param name="request">Client activation request.</param>
    /// <returns>Sanctioned extra Vitae (0 for traditions that forbid bonus Vitae).</returns>
    /// <exception cref="InvalidOperationException">When <paramref name="request"/> violates tradition rules.</exception>
    int ResolveExtraVitaeSpend(BeginRiteActivationRequest request);

    /// <summary>
    /// Adjusts Blood Sympathy ritual bonus dice (Crúac doubles the base bonus per V:tR 2e p. 153).
    /// </summary>
    /// <param name="baseBonus">Bonus from lineage degree and effective sympathy range.</param>
    /// <returns>Dice added to the pool after tradition adjustment.</returns>
    int AdjustRitualBloodSympathyBonus(int baseBonus);

    /// <summary>
    /// Whether this activation should raise a degeneration check after costs (Necromancy at Humanity 7+).
    /// </summary>
    /// <param name="character">The ritualist after cost application (same entity instance the orchestrator holds).</param>
    /// <returns><see langword="true"/> when the UI and domain should surface a degeneration check.</returns>
    bool ShouldRaiseDegenerationCheck(Character character);
}
