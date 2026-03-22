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
}
