namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Storyteller queue entry for illicit asset procurement.
/// </summary>
public sealed record PendingAssetProcurementDto(
    int Id,
    string CharacterName,
    string AssetName,
    int Quantity,
    DateTimeOffset RequestedAt,
    string? PlayerNote);
