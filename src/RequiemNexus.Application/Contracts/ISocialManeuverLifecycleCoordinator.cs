using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Shared Social maneuver lifecycle: tracked load with hostile-week resolution, SignalR fan-out, and idempotent condition application.
/// </summary>
public interface ISocialManeuverLifecycleCoordinator
{
    /// <summary>
    /// Loads a maneuver for mutation, applying hostile-week failure when due, and returns the tracked entity.
    /// </summary>
    /// <param name="db">Caller-owned context.</param>
    /// <param name="maneuverId">Primary key.</param>
    /// <returns>Tracked <see cref="SocialManeuver"/>.</returns>
    Task<SocialManeuver> LoadManeuverForMutationAsync(ApplicationDbContext db, int maneuverId);

    /// <summary>
    /// Publishes a campaign-wide update with goal text redacted, and a Storyteller-only copy with the full goal.
    /// </summary>
    /// <param name="db">Caller-owned context.</param>
    /// <param name="maneuverId">Maneuver to project.</param>
    Task PublishManeuverUpdateAsync(ApplicationDbContext db, int maneuverId);

    /// <summary>
    /// Applies a social condition if the character does not already have an active instance of that type.
    /// </summary>
    /// <param name="db">Caller-owned context (used only for the existence check).</param>
    /// <param name="characterId">Target PC.</param>
    /// <param name="type">Condition to apply.</param>
    /// <param name="descriptionOverride">Optional narrative description.</param>
    /// <param name="actingUserId">User id passed through to <see cref="IConditionService"/> (Masquerade).</param>
    Task ApplySocialConditionIfAbsentAsync(
        ApplicationDbContext db,
        int characterId,
        ConditionType type,
        string? descriptionOverride,
        string actingUserId);
}
