namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// Status of a character's bloodline application or membership.
/// </summary>
public enum BloodlineStatus
{
    /// <summary>Awaiting Storyteller approval.</summary>
    Pending,

    /// <summary>Approved and active.</summary>
    Active,

    /// <summary>Rejected by Storyteller.</summary>
    Rejected,
}
