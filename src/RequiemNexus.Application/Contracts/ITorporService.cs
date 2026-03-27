using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Torpor state transitions and starvation interval checks for vampires in torpor.
/// </summary>
public interface ITorporService
{
    /// <summary>
    /// Enters torpor. Sets <c>TorporSince</c> and resolves any active Beast tilt.
    /// </summary>
    Task<Result<Unit>> EnterTorporAsync(
        int characterId,
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Awakens from torpor. Costs 1 Vitae unless <paramref name="narrativeAwakening"/> is true (ST-confirmed anchor moment).
    /// </summary>
    Task<Result<Unit>> AwakenFromTorporAsync(
        int characterId,
        string userId,
        bool narrativeAwakening,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// When the starvation milestone has elapsed, records a notification (updates <c>LastStarvationNotifiedAt</c>). No-op if not due or BP 10.
    /// </summary>
    Task CheckStarvationIntervalAsync(int characterId, CancellationToken cancellationToken = default);
}
