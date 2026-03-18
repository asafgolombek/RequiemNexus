namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Summary of a bloodline for the Apply for Bloodline dialog.
/// </summary>
/// <param name="Id">Primary key of <see cref="Data.Models.BloodlineDefinition"/>.</param>
/// <param name="Name">Bloodline name.</param>
/// <param name="Description">Bloodline description.</param>
public record BloodlineSummaryDto(int Id, string Name, string Description);
