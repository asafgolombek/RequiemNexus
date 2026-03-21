namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Storyteller-only NPC encounter roll outcome (not broadcast to players).
/// </summary>
/// <param name="Successes">Total successes on the roll.</param>
/// <param name="DiceRolled">Individual die results (1–10).</param>
/// <param name="PoolDescription">Human-readable pool (e.g. Wits + Stealth (8)).</param>
/// <param name="IsExceptionalSuccess">True when successes meet exceptional threshold.</param>
/// <param name="IsDramaticFailure">True on dramatic failure.</param>
public record NpcEncounterRollResultDto(
    int Successes,
    IReadOnlyList<int> DiceRolled,
    string PoolDescription,
    bool IsExceptionalSuccess,
    bool IsDramaticFailure);
