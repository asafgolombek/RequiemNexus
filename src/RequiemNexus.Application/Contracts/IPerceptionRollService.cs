using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Storyteller-only perception rolls that never persist or broadcast to players.
/// </summary>
public interface IPerceptionRollService
{
    /// <summary>
    /// Rolls Wits + Composure (default) or Wits + Awareness for the character.
    /// </summary>
    /// <param name="characterId">Character to read traits from.</param>
    /// <param name="useAwareness">When true, uses Wits + Awareness skill; otherwise Wits + Composure.</param>
    /// <param name="penaltyDice">Dice removed from the pool (e.g. environmental penalties).</param>
    /// <param name="storyTellerUserId">Calling Storyteller user id.</param>
    /// <returns>Roll outcome for ST display only.</returns>
    Task<PerceptionRollResultDto> RollPerceptionAsync(
        int characterId,
        bool useAwareness,
        int penaltyDice,
        string storyTellerUserId);
}
