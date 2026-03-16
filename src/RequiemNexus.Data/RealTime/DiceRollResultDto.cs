namespace RequiemNexus.Data.RealTime;

/// <summary>
/// Represents the result of a dice roll broadcast to a session.
/// </summary>
/// <param name="PlayerName">Display name of the player who rolled.</param>
/// <param name="RolledByUserId">AspNetUsers Id of the player who rolled.</param>
/// <param name="CharacterId">Primary key of the character who rolled (optional).</param>
/// <param name="PoolDescription">Description of the dice pool (e.g., "Strength + Brawl").</param>
/// <param name="Successes">Total number of successes rolled.</param>
/// <param name="IsExceptionalSuccess">True if 5 or more successes were rolled.</param>
/// <param name="IsDramaticFailure">True if a chance die rolled a 1.</param>
/// <param name="DiceRolled">The raw results of every die rolled, including explosions.</param>
/// <param name="RolledAt">Timestamp of when the roll occurred.</param>
public record DiceRollResultDto(
    string PlayerName,
    string RolledByUserId,
    int? CharacterId,
    string PoolDescription,
    int Successes,
    bool IsExceptionalSuccess,
    bool IsDramaticFailure,
    IEnumerable<int> DiceRolled,
    DateTimeOffset RolledAt);
