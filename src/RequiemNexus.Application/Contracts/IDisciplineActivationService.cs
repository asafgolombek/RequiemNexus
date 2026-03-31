using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Handles Discipline power activation: pool resolution, cost deduction, and dice-feed publication.
/// Separated from <see cref="ICharacterDisciplineService"/> (XP acquisition) to keep each contract focused.
/// </summary>
public interface IDisciplineActivationService
{
    /// <summary>
    /// Resolves the dice pool for a power WITHOUT spending resources.
    /// Returns 0 when the power has no <c>PoolDefinitionJson</c>.
    /// Uses sync pool resolution (no passive modifiers) — preview only.
    /// </summary>
    /// <param name="characterId">The character activating the power.</param>
    /// <param name="disciplinePowerId">The catalogue power id.</param>
    /// <param name="userId">The authenticated user (owner or Storyteller).</param>
    /// <returns>Preview dice pool size, or 0 when there is no rollable pool.</returns>
    Task<int> ResolveActivationPoolAsync(int characterId, int disciplinePowerId, string userId);

    /// <summary>
    /// Deducts the activation cost, resolves the full pool (with passive modifiers),
    /// and returns the resolved pool size for the dice roller (which publishes to the feed when the player rolls).
    /// Throws <see cref="InvalidOperationException"/> when resources are insufficient,
    /// the power has no <c>PoolDefinitionJson</c>, or the character lacks the required rating.
    /// </summary>
    /// <param name="characterId">The character activating the power.</param>
    /// <param name="disciplinePowerId">The catalogue power id.</param>
    /// <param name="userId">The authenticated user (owner or Storyteller).</param>
    /// <param name="resourceChoice">
    /// Required when the parsed cost is <see cref="ActivationCost.IsPlayerChoiceVitaeOrWillpower"/>; otherwise ignored.
    /// </param>
    /// <returns>The modifier-aware dice pool size for the roll UI.</returns>
    Task<int> ActivatePowerAsync(
        int characterId,
        int disciplinePowerId,
        string userId,
        DisciplineActivationResourceChoice? resourceChoice = null);
}
