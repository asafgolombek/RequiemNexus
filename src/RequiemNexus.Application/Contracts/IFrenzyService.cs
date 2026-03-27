using RequiemNexus.Application.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Frenzy and Rötschreck resistance rolls (Vampire: The Requiem 2e).
/// </summary>
public interface IFrenzyService
{
    /// <summary>
    /// Executes a frenzy save for the character. Pool: Resolve + Blood Potency. On failure, applies Frenzy or Rötschreck tilt as appropriate.
    /// </summary>
    Task<Result<FrenzySaveResult>> RollFrenzySaveAsync(
        int characterId,
        string userId,
        FrenzyTrigger trigger,
        bool spendWillpower,
        CancellationToken cancellationToken = default);
}
