namespace RequiemNexus.Application.DTOs;

/// <summary>
/// High-level procurement path.
/// </summary>
public enum AssetProcurementOutcome
{
    /// <summary>Item was added immediately (Resources sufficient, not illicit).</summary>
    AddedImmediately,

    /// <summary>Player must roll using the procurement pool from the start result.</summary>
    RequiresProcurementRoll,

    /// <summary>Storyteller must approve in Glimpse (illicit).</summary>
    AwaitingStorytellerApproval,

    /// <summary>Cannot proceed; UI should show the start result message only (no roll, no pending row).</summary>
    Blocked,
}
