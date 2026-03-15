using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages the creation and retrieval of persistent public roll results.
/// </summary>
public interface IPublicRollService
{
    /// <summary>
    /// Creates a shareable record of a dice roll.
    /// </summary>
    /// <param name="userId">The player sharing the roll.</param>
    /// <param name="chronicleId">Optional campaign context.</param>
    /// <param name="poolDescription">Description of the pool.</param>
    /// <param name="roll">The result to share.</param>
    /// <returns>The generated slug for the roll.</returns>
    Task<string> ShareRollAsync(string userId, int? chronicleId, string poolDescription, DiceRollResultDto roll);

    /// <summary>
    /// Retrieves a shared roll result by its slug.
    /// </summary>
    Task<PublicRoll?> GetRollBySlugAsync(string slug);
}
