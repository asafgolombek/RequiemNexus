namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Outcome of starting a procurement attempt.
/// </summary>
public sealed record AssetProcurementStartResult(
    AssetProcurementOutcome Outcome,
    int? PendingProcurementId,
    string? Message);
