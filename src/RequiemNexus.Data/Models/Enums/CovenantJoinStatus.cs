namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// Status of a character's covenant join application.
/// </summary>
public enum CovenantJoinStatus
{
    /// <summary>Awaiting Storyteller approval.</summary>
    Pending,

    /// <summary>Approved and active member.</summary>
    Approved,

    /// <summary>Rejected by Storyteller.</summary>
    Rejected,
}
