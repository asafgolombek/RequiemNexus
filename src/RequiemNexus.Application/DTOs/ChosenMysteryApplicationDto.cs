namespace RequiemNexus.Application.DTOs;

/// <summary>
/// A character's pending Chosen Mystery (Scale) selection awaiting Storyteller approval.
/// </summary>
/// <param name="CharacterId">Character who requested the mystery.</param>
/// <param name="CharacterName">Display name of the character.</param>
/// <param name="ScaleId">Pending scale definition id.</param>
/// <param name="ScaleName">Display name of the scale (e.g. Coil of the Ascendant).</param>
public record ChosenMysteryApplicationDto(
    int CharacterId,
    string CharacterName,
    int ScaleId,
    string ScaleName);
