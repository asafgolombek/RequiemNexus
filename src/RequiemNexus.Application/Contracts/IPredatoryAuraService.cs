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
    /// Resolves a passive Predatory Aura (Blood Potency contest, Shaken on a decisive loser). Storyteller only.
    /// When <paramref name="encounterId"/> is set, skips if the Kindred pair already contested in that encounter.
    /// </summary>
    /// <returns>The contest result, or <see langword="null"/> when skipped as a duplicate in-encounter contest.</returns>
    Task<Result<PredatoryAuraContestResultDto?>> ResolvePassiveContestAsync(
        int chronicleId,
        int vampireAId,
        int vampireBId,
        string storytellerUserId,
        int? encounterId);

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
