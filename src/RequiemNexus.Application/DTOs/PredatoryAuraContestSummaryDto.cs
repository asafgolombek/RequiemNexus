namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Compact read model for Predatory Aura history tables (Storyteller Glimpse).
/// </summary>
public record PredatoryAuraContestSummaryDto(
    int Id,
    int AttackerCharacterId,
    int DefenderCharacterId,
    string AttackerName,
    string DefenderName,
    int AttackerSuccesses,
    int DefenderSuccesses,
    int? WinnerCharacterId,
    string OutcomeApplied,
    DateTime ResolvedAt);
