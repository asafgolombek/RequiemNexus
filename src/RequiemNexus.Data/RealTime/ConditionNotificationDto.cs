namespace RequiemNexus.Data.RealTime;

/// <summary>
/// Push notification payload when a condition or tilt changes on a character.
/// </summary>
/// <param name="CharacterId">Affected character.</param>
/// <param name="Name">Display name of the condition or tilt.</param>
/// <param name="IsTilt">True when the notification refers to a tilt.</param>
/// <param name="IsRemoval">True when the effect was cleared.</param>
public record ConditionNotificationDto(
    int CharacterId,
    string Name,
    bool IsTilt,
    bool IsRemoval);
