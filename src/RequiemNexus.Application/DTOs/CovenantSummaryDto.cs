namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Summary of a Covenant for selection dialogs.
/// </summary>
public record CovenantSummaryDto
{
    /// <summary>Primary key of the covenant definition.</summary>
    public required int Id { get; init; }

    /// <summary>Covenant name (e.g. "The Invictus").</summary>
    public required string Name { get; init; }

    /// <summary>Brief thematic description.</summary>
    public required string Description { get; init; }
}
