using RequiemNexus.Data.Models;

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
    /// Evaluates whether the character's current stains cross the degeneration threshold
    /// for their Humanity dot. If so, raises a degeneration check domain event.
    /// </summary>
    /// <param name="characterId">The character to evaluate.</param>
    /// <param name="userId">The authenticated user (owner or Storyteller).</param>
    Task EvaluateStainsAsync(int characterId, string userId);
}
