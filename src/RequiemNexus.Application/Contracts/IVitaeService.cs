using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Authoritative Vitae mutations on <see cref="RequiemNexus.Data.Models.Character"/> entities.
/// </summary>
public interface IVitaeService
{
    /// <summary>
    /// Spends <paramref name="amount"/> Vitae. Dispatches <see cref="RequiemNexus.Domain.Events.VitaeDepletedEvent"/> when <c>CurrentVitae</c> reaches 0 after the spend.
    /// </summary>
    Task<Result<int>> SpendVitaeAsync(
        int characterId,
        string userId,
        int amount,
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gains <paramref name="amount"/> Vitae, capped at <c>MaxVitae</c>.
    /// </summary>
    Task<Result<int>> GainVitaeAsync(
        int characterId,
        string userId,
        int amount,
        string reason,
        CancellationToken cancellationToken = default);
}
