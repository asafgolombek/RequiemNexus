namespace RequiemNexus.Data.RealTime;

/// <summary>
/// Presence information for a player currently in a session.
/// </summary>
/// <param name="UserId">AspNetUsers Id of the player.</param>
/// <param name="UserName">The display name or email of the player.</param>
/// <param name="CharacterId">The ID of the character the player is currently using (if any).</param>
/// <param name="IsOnline">Whether the player is currently connected to the hub.</param>
public record PlayerPresenceDto(
    string UserId,
    string UserName,
    int? CharacterId,
    bool IsOnline);
