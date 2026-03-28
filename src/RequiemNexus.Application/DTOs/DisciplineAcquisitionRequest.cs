namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Carries the parameters for a Discipline purchase or upgrade.
/// Replaces bare (characterId, disciplineId, rating) parameters on <see cref="Contracts.ICharacterDisciplineService"/>.
/// </summary>
/// <param name="CharacterId">The character gaining or raising the Discipline.</param>
/// <param name="DisciplineId">The catalogue Discipline id.</param>
/// <param name="TargetRating">The rating after purchase (1–5).</param>
/// <param name="AcquisitionAcknowledgedByST">When true, the Storyteller has acknowledged a soft gate; verified server-side.</param>
public sealed record DisciplineAcquisitionRequest(
    int CharacterId,
    int DisciplineId,
    int TargetRating,
    bool AcquisitionAcknowledgedByST = false);
