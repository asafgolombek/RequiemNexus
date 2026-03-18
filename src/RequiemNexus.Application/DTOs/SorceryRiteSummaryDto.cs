using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Summary of a sorcery rite for eligibility display.
/// </summary>
/// <param name="Id">Primary key of <see cref="Data.Models.SorceryRiteDefinition"/>.</param>
/// <param name="Name">Rite name.</param>
/// <param name="Level">Rite level (1–5).</param>
/// <param name="SorceryType">Cruac or Theban.</param>
/// <param name="XpCost">XP cost to learn.</param>
/// <param name="CovenantName">Required covenant name.</param>
public record SorceryRiteSummaryDto(
    int Id,
    string Name,
    int Level,
    SorceryType SorceryType,
    int XpCost,
    string CovenantName);
