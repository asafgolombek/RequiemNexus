namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Summary of an Ordo Dracul Mystery Scale for selection.
/// </summary>
public record ScaleSummaryDto
{
    /// <summary>Primary key of the scale definition.</summary>
    public required int Id { get; init; }

    /// <summary>The name of the scale (e.g. "The Coil of the Locust").</summary>
    public required string Name { get; init; }

    /// <summary>Brief summary of what the scale represents.</summary>
    public required string Description { get; init; }

    /// <summary>The name of the Mystery this scale belongs to.</summary>
    public required string MysteryName { get; init; }

    /// <summary>The maximum tier level available for this scale.</summary>
    public required int MaxLevel { get; init; }
}
