namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Summary of a bloodline for selection dialogs.
/// </summary>
public record BloodlineSummaryDto
{
    /// <summary>Primary key of the bloodline definition.</summary>
    public required int Id { get; init; }

    /// <summary>Bloodline name (e.g. "Khaibit").</summary>
    public required string Name { get; init; }

    /// <summary>Brief thematic description.</summary>
    public required string Description { get; init; }
}
