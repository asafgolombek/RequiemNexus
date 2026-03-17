namespace RequiemNexus.Application.DTOs;

/// <summary>
/// A pending bloodline application for the Storyteller Glimpse dashboard.
/// </summary>
/// <param name="CharacterBloodlineId">Primary key of <see cref="Data.Models.CharacterBloodline"/>.</param>
/// <param name="CharacterId">Character who applied.</param>
/// <param name="CharacterName">Character's in-game name.</param>
/// <param name="BloodlineName">Requested bloodline name.</param>
/// <param name="AppliedAt">When the application was submitted.</param>
public record BloodlineApplicationDto(
    int CharacterBloodlineId,
    int CharacterId,
    string CharacterName,
    string BloodlineName,
    DateTime AppliedAt);
