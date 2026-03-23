using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Result of a resolved Predatory Aura Lash Out, including audit identifiers for the sheet and session clients.
/// </summary>
public record PredatoryAuraContestResultDto(
    int ContestId,
    int ChronicleId,
    int AttackerCharacterId,
    string AttackerName,
    int DefenderCharacterId,
    string DefenderName,
    int AttackerBloodPotency,
    int DefenderBloodPotency,
    int AttackerSuccesses,
    int DefenderSuccesses,
    PredatoryAuraOutcome Outcome,
    int? WinnerCharacterId,
    string OutcomeApplied,
    string? AppliedConditionToLoser);
