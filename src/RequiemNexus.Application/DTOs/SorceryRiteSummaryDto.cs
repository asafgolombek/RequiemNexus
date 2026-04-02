using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Summary of a sorcery rite for eligibility display.
/// </summary>
/// <param name="Id">Primary key of <see cref="Data.Models.SorceryRiteDefinition"/>.</param>
/// <param name="Name">Rite name.</param>
/// <param name="Level">Rite level (1–5).</param>
/// <param name="SorceryType">Tradition (Crúac, Theban, Necromancy).</param>
/// <param name="XpCost">XP cost to learn.</param>
/// <param name="CovenantName">Covenant name, clan gate summary, or "—" when not covenant-gated.</param>
/// <param name="TargetSuccesses">Extended-action successes required (V:tR 2e ritual casting).</param>
public record SorceryRiteSummaryDto(
    int Id,
    string Name,
    int Level,
    SorceryType SorceryType,
    int XpCost,
    string CovenantName,
    int TargetSuccesses);
