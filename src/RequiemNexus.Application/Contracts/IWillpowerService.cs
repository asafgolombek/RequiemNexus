using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Authoritative Willpower mutations on <see cref="RequiemNexus.Data.Models.Character"/> entities.
/// </summary>
public interface IWillpowerService
{
    /// <summary>
    /// Spends <paramref name="amount"/> Willpower.
    /// </summary>
    Task<Result<int>> SpendWillpowerAsync(
        int characterId,
        string userId,
        int amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recovers <paramref name="amount"/> Willpower, capped at <c>MaxWillpower</c>.
    /// </summary>
    Task<Result<int>> RecoverWillpowerAsync(
        int characterId,
        string userId,
        int amount,
        CancellationToken cancellationToken = default);
}
