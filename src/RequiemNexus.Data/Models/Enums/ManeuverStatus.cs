namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// Lifecycle state of a social maneuver against an NPC.
/// </summary>
public enum ManeuverStatus
{
    /// <summary>In progress; Doors may still be opened or forced.</summary>
    Active = 0,

    /// <summary>Final Door opened without burning the relationship.</summary>
    Succeeded = 1,

    /// <summary>Ended by book failure (e.g. hostile for a week).</summary>
    Failed = 2,

    /// <summary>Forcing Doors failed; initiator cannot use Social maneuvering on this victim again.</summary>
    Burnt = 3,
}
