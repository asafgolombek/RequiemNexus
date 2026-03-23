namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Storyteller request to create a ghoul retainer.
/// Exactly one of <see cref="RegnantCharacterId"/>, <see cref="RegnantNpcId"/>, or non-empty
/// <see cref="RegnantDisplayName"/> must be provided.
/// </summary>
public record CreateGhoulRequest(
    int ChronicleId,
    string Name,
    int? RegnantCharacterId,
    int? RegnantNpcId,
    string? RegnantDisplayName,
    int? ApparentAge,
    int? ActualAge,
    string? Notes);
