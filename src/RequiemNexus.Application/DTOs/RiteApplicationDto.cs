namespace RequiemNexus.Application.DTOs;

using RequiemNexus.Domain.Enums;

/// <summary>
/// A pending rite learning application for the Storyteller Glimpse dashboard.
/// </summary>
/// <param name="CharacterRiteId">Primary key of <see cref="Data.Models.CharacterRite"/>.</param>
/// <param name="CharacterId">Character who requested.</param>
/// <param name="CharacterName">Character's in-game name.</param>
/// <param name="RiteName">Requested rite name.</param>
/// <param name="SorceryType">Crúac or Theban.</param>
/// <param name="Level">Rite level (1–5).</param>
/// <param name="XpCost">XP cost of the rite.</param>
/// <param name="AppliedAt">When the request was submitted.</param>
public record RiteApplicationDto(
    int CharacterRiteId,
    int CharacterId,
    string CharacterName,
    string RiteName,
    SorceryType SorceryType,
    int Level,
    int XpCost,
    DateTime AppliedAt);
