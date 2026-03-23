namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Read model for a Blood Bond row with display fields for UI.
/// </summary>
/// <param name="Id">Primary key.</param>
/// <param name="ChronicleId">Campaign / chronicle scope.</param>
/// <param name="ThrallCharacterId">The bound character.</param>
/// <param name="ThrallName">Thrall display name.</param>
/// <param name="RegnantCharacterId">PC regnant, if any.</param>
/// <param name="RegnantNpcId">NPC regnant, if any.</param>
/// <param name="RegnantDisplayName">Resolved regnant label for tables and sheets.</param>
/// <param name="Stage">Bond stage 1–3.</param>
/// <param name="LastFedAt">UTC timestamp of last feeding from this regnant.</param>
/// <param name="IsFading">True when <see cref="RequiemNexus.Domain.BloodBondRules.IsFading"/> applies.</param>
/// <param name="ActiveConditionName">Human-readable name of the condition imposed by the current stage.</param>
public record BloodBondDto(
    int Id,
    int ChronicleId,
    int ThrallCharacterId,
    string ThrallName,
    int? RegnantCharacterId,
    int? RegnantNpcId,
    string RegnantDisplayName,
    int Stage,
    DateTime? LastFedAt,
    bool IsFading,
    string ActiveConditionName);
