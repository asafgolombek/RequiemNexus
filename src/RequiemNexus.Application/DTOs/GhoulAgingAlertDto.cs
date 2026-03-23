namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Storyteller alert for a ghoul that is past the Vitae feeding interval.
/// </summary>
public record GhoulAgingAlertDto(
    int GhoulId,
    string GhoulName,
    string RegnantDisplayName,
    DateTime? LastFedAt,
    int OverdueMonths);
