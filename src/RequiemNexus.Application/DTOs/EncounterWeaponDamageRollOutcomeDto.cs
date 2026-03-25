namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Result of a player-owned weapon damage roll during an encounter, returned to the SignalR caller after publishing to the chronicle.
/// </summary>
/// <param name="Successes">Count of successes on the weapon damage pool.</param>
/// <param name="IsExceptionalSuccess">True when successes meet exceptional threshold.</param>
/// <param name="IsDramaticFailure">True when a chance die dramatic failure occurred.</param>
/// <param name="DiceRolled">Individual die results (including explosions).</param>
/// <param name="PoolDescription">Label broadcast to the session log.</param>
public record EncounterWeaponDamageRollOutcomeDto(
    int Successes,
    bool IsExceptionalSuccess,
    bool IsDramaticFailure,
    int[] DiceRolled,
    string PoolDescription);
