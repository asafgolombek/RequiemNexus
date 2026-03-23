namespace RequiemNexus.Application.DTOs;

/// <summary>
/// A single node in a character's lineage graph (sire or childe).
/// </summary>
/// <param name="CharacterId">Player character id when the kin is a PC; otherwise null.</param>
/// <param name="NpcId">Chronicle NPC id when the kin is an NPC; otherwise null.</param>
/// <param name="DisplayName">Resolved display name for the kin.</param>
/// <param name="BloodPotency">Blood Potency when known (PC sheet); null for unlinked or NPCs without stats.</param>
/// <param name="BloodSympathyRating">Derived rating when <see cref="BloodPotency"/> is known; otherwise null.</param>
/// <param name="DegreeOfSeparation">Graph distance from the focal character (1 = direct sire or childe).</param>
public record KinNodeDto(
    int? CharacterId,
    int? NpcId,
    string DisplayName,
    int? BloodPotency,
    int? BloodSympathyRating,
    int DegreeOfSeparation);
