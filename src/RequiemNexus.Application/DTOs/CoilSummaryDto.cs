namespace RequiemNexus.Application.DTOs;

public record CoilSummaryDto(
    int Id,
    string Name,
    string Description,
    int Level,
    int ScaleId,
    string ScaleName,
    int? PrerequisiteCoilId,
    int XpCost,
    string? RollDescription,
    bool IsChosenMystery);
