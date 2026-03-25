using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Applies structured damage and Vitae-driven healing to a character health track (Phase 14).
/// </summary>
public interface ICharacterHealthService
{
    /// <summary>
    /// Applies <paramref name="instances"/> hits of <paramref name="kind"/> after ownership / ST access checks.
    /// </summary>
    /// <param name="characterId">Defender character.</param>
    /// <param name="userId">Authenticated user (owner or Storyteller).</param>
    /// <param name="kind">Bashing, lethal, or aggravated.</param>
    /// <param name="instances">Number of sequential applications (overflow rules apply per hit).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ApplyStructuredDamageAsync(
        int characterId,
        string userId,
        HealthDamageKind kind,
        int instances,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies damage derived from a resolved melee <see cref="AttackResult"/>.
    /// </summary>
    /// <param name="characterId">Defender character.</param>
    /// <param name="userId">Authenticated user.</param>
    /// <param name="attackResult">Structured attack outcome.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ApplyDamageFromAttackAsync(
        int characterId,
        string userId,
        AttackResult attackResult,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Spends Vitae to remove bashing damage from the rightmost boxes.
    /// </summary>
    /// <param name="characterId">Character to heal.</param>
    /// <param name="userId">Authenticated user.</param>
    /// <param name="boxCount">Maximum boxes to heal.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of boxes actually healed, or a player-safe error.</returns>
    Task<Result<int>> TryFastHealBashingWithVitaeAsync(
        int characterId,
        string userId,
        int boxCount,
        CancellationToken cancellationToken = default);
}
