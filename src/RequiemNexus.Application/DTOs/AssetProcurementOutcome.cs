namespace RequiemNexus.Application.DTOs;

/// <summary>
/// High-level procurement path.
/// </summary>
public enum AssetProcurementOutcome
{
    /// <summary>Item was added immediately (listed, not illicit).</summary>
    AddedImmediately,

    /// <summary>Storyteller must approve in Glimpse (illicit).</summary>
    AwaitingStorytellerApproval,

    /// <summary>Cannot proceed; UI should show the start result message only (no roll, no pending row).</summary>
    Blocked,
}
