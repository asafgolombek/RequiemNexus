namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Summary of a covenant for the Apply for Covenant dialog.
/// </summary>
/// <param name="Id">Primary key of <see cref="Data.Models.CovenantDefinition"/>.</param>
/// <param name="Name">Covenant name.</param>
/// <param name="Description">Covenant description.</param>
public record CovenantSummaryDto(int Id, string Name, string Description);
