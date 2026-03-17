namespace RequiemNexus.Data.RealTime;

/// <summary>
/// Persistence contract for ephemeral session state stored in Redis.
/// </summary>
public interface ISessionStateRepository
{
    /// <summary>
    /// Creates the initial session info record with a TTL.
    /// </summary>
    Task CreateSessionAsync(int chronicleId, string stUserId, TimeSpan ttl);

    /// <summary>
    /// Renews the TTL for an active session.
    /// </summary>
    Task RefreshSessionAsync(int chronicleId, TimeSpan ttl);

    /// <summary>
    /// Deletes all Redis keys associated with a chronicle's session.
    /// </summary>
    Task DeleteSessionAsync(int chronicleId);

    /// <summary>
    /// Checks if a session is currently active for the chronicle.
    /// </summary>
    Task<bool> SessionExistsAsync(int chronicleId);

    /// <summary>
    /// Adds a player to the session's presence SET.
    /// </summary>
    Task AddPlayerAsync(int chronicleId, PlayerPresenceDto player);

    /// <summary>
    /// Removes a player from the session's presence SET.
    /// </summary>
    Task RemovePlayerAsync(int chronicleId, string userId);

    /// <summary>
    /// Retrieves all currently active players in the session.
    /// </summary>
    Task<IEnumerable<PlayerPresenceDto>> GetPlayersAsync(int chronicleId);

    /// <summary>
    /// Maps a user to a chronicle ID for lookup during disconnect.
    /// </summary>
    Task MapUserToSessionAsync(string userId, int chronicleId);

    /// <summary>
    /// Removes the user-to-session mapping.
    /// </summary>
    Task UnmapUserFromSessionAsync(string userId);

    /// <summary>
    /// Gets the chronicle ID associated with a user.
    /// </summary>
    Task<int?> GetUserSessionAsync(string userId);

    /// <summary>
    /// Appends a dice roll result to the session's history LIST.
    /// Caps the history at 100 entries.
    /// </summary>
    Task AddRollAsync(int chronicleId, DiceRollResultDto roll);

    /// <summary>
    /// Retrieves the recent roll history for a session.
    /// </summary>
    Task<IEnumerable<DiceRollResultDto>> GetRollHistoryAsync(int chronicleId);

    /// <summary>
    /// Updates the initiative ZSET for a session.
    /// </summary>
    Task UpdateInitiativeAsync(int chronicleId, IEnumerable<InitiativeEntryDto> entries);

    /// <summary>
    /// Retrieves the current initiative state.
    /// </summary>
    Task<IEnumerable<InitiativeEntryDto>> GetInitiativeAsync(int chronicleId);
}
