using RequiemNexus.Application.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Executes a hunt roll for a character based on their Predator Type.
/// Resolves the canonical pool, applies optional territory bonus, gains Vitae, records the result.
/// </summary>
public interface IHuntingService
{
    /// <summary>
    /// Rolls a hunt for <paramref name="characterId"/>.
    /// If <paramref name="territoryId"/> is provided, <see cref="RequiemNexus.Data.Models.FeedingTerritory.Rating"/> adds bonus dice.
    /// Masquerade: caller must own the character or be the campaign Storyteller (see <see cref="IAuthorizationHelper.RequireCharacterAccessAsync"/>).
    /// </summary>
    Task<Result<HuntResult>> ExecuteHuntAsync(
        int characterId,
        string userId,
        int? territoryId = null,
        CancellationToken cancellationToken = default);
}
