namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Server-only perception roll outcome returned to the Storyteller (never broadcast to players).
/// </summary>
/// <param name="Successes">Total successes on the roll.</param>
/// <param name="DiceRolled">Individual die results (1–10).</param>
/// <param name="PoolDescription">Human-readable pool (e.g. Wits + Composure).</param>
/// <param name="IsExceptionalSuccess">True when successes meet exceptional threshold.</param>
/// <param name="IsDramaticFailure">True on dramatic failure.</param>
public record PerceptionRollResultDto(
    int Successes,
    IReadOnlyList<int> DiceRolled,
    string PoolDescription,
    bool IsExceptionalSuccess,
    bool IsDramaticFailure);
