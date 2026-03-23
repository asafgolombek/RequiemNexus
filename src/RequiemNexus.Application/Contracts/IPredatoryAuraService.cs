using RequiemNexus.Application.DTOs;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Orchestrates Predatory Aura (Lash Out) contests: server-side Blood Potency rolls, Shaken Condition on the loser, audit rows, and session broadcasts.
/// </summary>
public interface IPredatoryAuraService
{
    /// <summary>
    /// Executes a deliberate Predatory Aura Lash Out between two characters in the same chronicle.
    /// Rolls each character's Blood Potency as the dice pool (not via <c>TraitResolver</c>).
    /// On a decisive result, applies Shaken to the loser with a <c>predatoryaura:{contestId}</c> source tag.
    /// </summary>
    /// <param name="chronicleId">Campaign scope; both characters must belong here.</param>
    /// <param name="attackerCharacterId">Initiating Kindred (must be owned by <paramref name="userId"/> unless they are the Storyteller).</param>
    /// <param name="defenderCharacterId">Target Kindred.</param>
    /// <param name="userId">Authenticated user.</param>
    /// <returns>Contest outcome and roll details, or a player-safe failure message.</returns>
    Task<Result<PredatoryAuraContestResultDto>> ResolveLashOutAsync(
        int chronicleId,
        int attackerCharacterId,
        int defenderCharacterId,
        string userId);

    /// <summary>
    /// Returns recent Predatory Aura contests in the chronicle (Storyteller only).
    /// </summary>
    /// <param name="chronicleId">Campaign id.</param>
    /// <param name="userId">Must be the campaign Storyteller.</param>
    /// <param name="take">Maximum rows (most recent first).</param>
    Task<IReadOnlyList<PredatoryAuraContestSummaryDto>> GetRecentContestsAsync(
        int chronicleId,
        string userId,
        int take = 25);
}
