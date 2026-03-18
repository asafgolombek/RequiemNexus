namespace RequiemNexus.Application.DTOs;

public record CoilApplicationDto(
    int CharacterCoilId,
    int CharacterId,
    string CharacterName,
    string CoilName,
    string ScaleName,
    int Level,
    int XpCost,
    DateTime AppliedAt);
