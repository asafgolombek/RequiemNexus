namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// Status of a character's rite learning request. Mirrors BloodlineStatus flow: Pending → Approved or Rejected.
/// </summary>
public enum RiteLearnStatus
{
    /// <summary>Awaiting Storyteller approval.</summary>
    Pending,

    /// <summary>Approved — character has learned the rite.</summary>
    Approved,

    /// <summary>Rejected by Storyteller.</summary>
    Rejected,
}
