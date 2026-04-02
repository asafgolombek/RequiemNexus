using RequiemNexus.Application.Models;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>Manages Humanity tracking, Crúac caps, and degeneration triggers.</summary>
public interface IHumanityService
{
    /// <summary>
    /// Returns the effective maximum Humanity for a character.
    /// Crúac permanently caps Humanity at 10 − CrúacRating.
    /// </summary>
    /// <param name="character">The character (must have Disciplines loaded for name-based Crúac lookup).</param>
    /// <returns>The highest Humanity dot the character may retain while their current Crúac rating applies.</returns>
    int GetEffectiveMaxHumanity(Character character);

    /// <summary>
    /// Clamps <paramref name="character"/>.Humanity to <see cref="GetEffectiveMaxHumanity"/> using Crúac dots from the database
    /// when Discipline navigations are not loaded (e.g. sheet save). Idempotent when already at or below the cap.
    /// </summary>
    /// <param name="character">Tracked character whose Humanity may be reduced.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnforceHumanityCapForPersistenceAsync(Character character, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether the character's current stains cross the degeneration threshold
    /// for their Humanity dot. If so, raises a degeneration check domain event.
    /// </summary>
    /// <param name="characterId">The character to evaluate.</param>
    /// <param name="userId">The authenticated user (owner or Storyteller).</param>
    Task EvaluateStainsAsync(int characterId, string userId);

    /// <summary>
    /// Performs a degeneration check: pool is Resolve + (7 − Humanity) with 10-again, or a single chance die when Humanity is 0.
    /// On success (≥1 success), clears all stains. On failure, removes one Humanity dot (minimum 0), clears stains; on dramatic failure, also applies <c>Guilty</c>.
    /// Publishes the roll to the chronicle dice feed when the character has a <c>CampaignId</c>.
    /// </summary>
    /// <param name="characterId">Character rolling degeneration.</param>
    /// <param name="userId">Authenticated user (owner or Storyteller).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outcome summary, or failure if the character does not exist.</returns>
    Task<Result<DegenerationRollOutcome>> ExecuteDegenerationRollAsync(
        int characterId,
        string userId,
        CancellationToken cancellationToken = default);
}
