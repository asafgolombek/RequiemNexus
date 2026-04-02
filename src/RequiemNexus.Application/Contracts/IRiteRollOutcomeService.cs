using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Applies V:tR 2e ritual roll outcome Conditions (Phase 19.5 P1-3). Authorization is enforced by <see cref="IConditionService"/>.
/// </summary>
public interface IRiteRollOutcomeService
{
    /// <summary>
    /// If the tradition and trigger map to a Condition, applies it via <see cref="IConditionService"/>; otherwise no-ops.
    /// </summary>
    Task ApplyRiteRollOutcomeAsync(
        int characterId,
        string userId,
        SorceryType tradition,
        RiteRollOutcomeTrigger trigger,
        CancellationToken cancellationToken = default);
}
