namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Status returned to the UI after attempting to purchase a catalog item (Phase 11).
/// </summary>
public enum AssetProcurementOutcome
{
    /// <summary>Resources dots > Availability; item added.</summary>
    AddedImmediately,

    /// <summary>Resources dots == Availability; once per chapter.</summary>
    AddedByReach,

    /// <summary>Illicit flag or ST-only item.</summary>
    AwaitingStorytellerApproval,

    /// <summary>Validation failed (e.g. no chronicle for illicit item).</summary>
    Blocked,
}
