using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Rolls the Blood Sympathy dice pool for a character attempting to sense kin.
/// Separated from <see cref="IKindredLineageService"/> to keep each service focused.
/// </summary>
public interface IBloodSympathyRollService
{
    /// <summary>
    /// Rolls the Blood Sympathy pool for a character attempting to locate kin.
    /// Validates lineage degree and Blood Potency range before rolling.
    /// </summary>
    Task<Result<RollResult>> RollBloodSympathyAsync(int characterId, int targetCharacterId, string userId);
}
