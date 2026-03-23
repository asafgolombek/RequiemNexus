namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Outcome of starting a catalog purchase attempt.
/// </summary>
public sealed record AssetProcurementStartResult(
    AssetProcurementOutcome Outcome,
    int? PendingProcurementId,
    string? Message);
