using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Application.RealTime;

/// <summary>
/// Orchestrates all session-related operations and broadcasts.
/// The interface sits in Application, but the Hub in Web will call it.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Starts a new play session for a chronicle. Validates that the caller is the Storyteller.
    /// </summary>
    /// <param name="userId">AspNetUsers Id of the Storyteller.</param>
    /// <param name="chronicleId">Primary key of the chronicle.</param>
    Task StartSessionAsync(string userId, int chronicleId);

    /// <summary>
    /// Explicitly ends a play session. Validates that the caller is the Storyteller.
    /// </summary>
    /// <param name="userId">AspNetUsers Id of the Storyteller.</param>
    /// <param name="chronicleId">Primary key of the chronicle.</param>
    Task EndSessionAsync(string userId, int chronicleId);

    /// <summary>
    /// Adds a player to an active session. Validates chronicle membership.
    /// </summary>
    /// <param name="userId">AspNetUsers Id of the player.</param>
    /// <param name="userName">Display name of the player.</param>
    /// <param name="chronicleId">Primary key of the chronicle.</param>
    /// <param name="characterId">ID of the character the player is using (optional).</param>
    /// <param name="connectionId">The SignalR connection ID.</param>
    Task JoinSessionAsync(string userId, string userName, int chronicleId, int? characterId, string connectionId);

    /// <summary>
    /// Removes a player from a session (usually on disconnect).
    /// </summary>
    /// <param name="userId">AspNetUsers Id of the player.</param>
    /// <param name="connectionId">The SignalR connection ID.</param>
    Task LeaveSessionAsync(string userId, string connectionId);

    /// <summary>
    /// Performs a server-side dice roll and broadcasts the result to the session.
    /// </summary>
    /// <param name="userId">AspNetUsers Id of the player rolling.</param>
    /// <param name="chronicleId">Primary key of the chronicle.</param>
    /// <param name="characterId">Primary key of the character who rolled (optional).</param>
    /// <param name="pool">Number of dice to roll.</param>
    /// <param name="description">Description of the pool (e.g., "Strength + Brawl").</param>
    /// <param name="tenAgain">True for 10-again rules.</param>
    /// <param name="nineAgain">True for 9-again rules.</param>
    /// <param name="eightAgain">True for 8-again rules.</param>
    /// <param name="isRote">True for rote actions.</param>
    Task RollDiceAsync(string userId, int chronicleId, int? characterId, int pool, string description, bool tenAgain, bool nineAgain, bool eightAgain, bool isRote);

    /// <summary>
    /// Updates the shared initiative tracker. Validates Storyteller role.
    /// </summary>
    /// <param name="userId">AspNetUsers Id of the caller.</param>
    /// <param name="chronicleId">Primary key of the chronicle.</param>
    /// <param name="entries">The new initiative order.</param>
    Task UpdateInitiativeAsync(string userId, int chronicleId, IEnumerable<InitiativeEntryDto> entries);

    /// <summary>
    /// Storyteller heartbeat to keep the session alive in Redis.
    /// </summary>
    /// <param name="userId">AspNetUsers Id of the Storyteller.</param>
    /// <param name="chronicleId">Primary key of the chronicle.</param>
    Task HeartbeatAsync(string userId, int chronicleId);

    /// <summary>
    /// Broadcasts the current character vitals to the chronicle group.
    /// </summary>
    /// <param name="characterId">Primary key of the character.</param>
    Task BroadcastCharacterUpdateAsync(int characterId);

    /// <summary>
    /// Broadcasts bloodline approval notification to the chronicle group.
    /// </summary>
    /// <param name="characterId">Primary key of the character whose bloodline was approved.</param>
    /// <param name="bloodlineName">Name of the approved bloodline.</param>
    Task BroadcastBloodlineApprovedAsync(int characterId, string bloodlineName);

    /// <summary>
    /// Broadcasts a chronicle state update to the group.
    /// </summary>
    /// <param name="patch">The delta update.</param>
    Task BroadcastChronicleUpdateAsync(ChronicleUpdateDto patch);

    /// <summary>
    /// Retrieves the full state of an active session.
    /// </summary>
    /// <param name="chronicleId">Primary key of the chronicle.</param>
    Task<SessionStateDto?> GetSessionStateAsync(int chronicleId);
}
