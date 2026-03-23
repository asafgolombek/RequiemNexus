namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Storyteller request to update a ghoul's narrative fields.
/// </summary>
public record UpdateGhoulRequest(
    int GhoulId,
    string Name,
    int? ApparentAge,
    int? ActualAge,
    string? Notes);
