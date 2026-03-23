namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Read model for a <see cref="RequiemNexus.Data.Models.Ghoul"/> with display fields for UI.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="ChronicleId">Campaign / chronicle scope.</param>
/// <param name="Name">Ghoul name.</param>
/// <param name="RegnantCharacterId">PC regnant, if any.</param>
/// <param name="RegnantNpcId">Chronicle NPC regnant, if any.</param>
/// <param name="RegnantDisplayName">Resolved regnant label.</param>
/// <param name="LastFedAt">UTC timestamp of last feeding.</param>
/// <param name="VitaeInSystem">Vitae held (0 or 1 for mortals).</param>
/// <param name="ApparentAge">Optional apparent age.</param>
/// <param name="ActualAge">Optional actual age.</param>
/// <param name="AccessibleDisciplineIds">Discipline IDs accessible at rating 1.</param>
/// <param name="AccessibleDisciplineNames">Resolved discipline names, same order as IDs where possible.</param>
/// <param name="Notes">ST notes.</param>
/// <param name="IsReleased">Whether the ghoul was released from service.</param>
/// <param name="CreatedAt">UTC creation time (used with <see cref="LastFedAt"/> for overdue calculations when never fed).</param>
/// <param name="IsAgingDue">True when <see cref="RequiemNexus.Domain.GhoulAgingRules.IsAgingDue"/> applies.</param>
/// <param name="OverdueMonthsAfterGrace">Full months past the feeding grace period; zero when not aging-due.</param>
/// <param name="DisciplineAccessEnforced">When true, <see cref="RegnantAllowedDisciplineIds"/> and <see cref="DisciplineAccessMaxCount"/> apply.</param>
/// <param name="DisciplineAccessMaxCount">Maximum selectable disciplines when <see cref="DisciplineAccessEnforced"/> (regnant Blood Potency).</param>
/// <param name="RegnantAllowedDisciplineIds">In-clan discipline IDs for the regnant PC; empty when not enforced.</param>
public record GhoulDto(
    int Id,
    int ChronicleId,
    string Name,
    int? RegnantCharacterId,
    int? RegnantNpcId,
    string RegnantDisplayName,
    DateTime? LastFedAt,
    int VitaeInSystem,
    int? ApparentAge,
    int? ActualAge,
    IReadOnlyList<int> AccessibleDisciplineIds,
    IReadOnlyList<string> AccessibleDisciplineNames,
    string? Notes,
    bool IsReleased,
    DateTime CreatedAt,
    bool IsAgingDue,
    int OverdueMonthsAfterGrace,
    bool DisciplineAccessEnforced,
    int DisciplineAccessMaxCount,
    IReadOnlyList<int> RegnantAllowedDisciplineIds);
