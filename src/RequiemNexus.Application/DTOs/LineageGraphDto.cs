namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Lineage snapshot for a character: focal stats, optional sire, and known childer.
/// </summary>
/// <param name="CharacterId">The focal character.</param>
/// <param name="CharacterName">Display name of the focal character.</param>
/// <param name="BloodPotency">Focal Blood Potency.</param>
/// <param name="BloodSympathyRating">Derived Blood Sympathy rating for the focal character.</param>
/// <param name="Sire">Linked or text sire, if any.</param>
/// <param name="Childer">Direct childer (PCs in the chronicle).</param>
public record LineageGraphDto(
    int CharacterId,
    string CharacterName,
    int BloodPotency,
    int BloodSympathyRating,
    KinNodeDto? Sire,
    IReadOnlyList<KinNodeDto> Childer);
