namespace RequiemNexus.Application.DTOs;

/// <summary>
/// A pending covenant application for the Storyteller Glimpse dashboard.
/// </summary>
/// <param name="CharacterId">Character who applied.</param>
/// <param name="CharacterName">Character's in-game name.</param>
/// <param name="CovenantName">Requested covenant name.</param>
/// <param name="AppliedAt">When the application was submitted.</param>
public record CovenantApplicationDto(
    int CharacterId,
    string CharacterName,
    string CovenantName,
    DateTime AppliedAt);
