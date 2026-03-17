using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Application.RealTime;

/// <summary>
/// Strongly-typed client interface for the SessionHub.
/// Declares all methods the server can broadcast to connected clients.
/// </summary>
public interface ISessionClient
{
    /// <summary>
    /// Broadcast when the Storyteller explicitly starts a new play session.
    /// </summary>
    Task SessionStarted();

    /// <summary>
    /// Broadcast when a session ends or auto-terminates.
    /// </summary>
    /// <param name="reason">Human-readable reason for termination.</param>
    Task SessionEnded(string reason);

    /// <summary>
    /// Broadcast when a player joins the session.
    /// </summary>
    /// <param name="player">The presence info of the joining player.</param>
    Task PlayerJoined(PlayerPresenceDto player);

    /// <summary>
    /// Broadcast when a player disconnects or leaves.
    /// </summary>
    /// <param name="userId">The AspNetUsers Id of the player who left.</param>
    Task PlayerLeft(string userId);

    /// <summary>
    /// Broadcasts a new dice roll result to the group.
    /// </summary>
    /// <param name="roll">The roll result details.</param>
    Task ReceiveDiceRoll(DiceRollResultDto roll);

    /// <summary>
    /// Hydrates a newly joined client with the recent roll history.
    /// </summary>
    /// <param name="history">The last N rolls for this session.</param>
    Task ReceiveRollHistory(IEnumerable<DiceRollResultDto> history);

    /// <summary>
    /// Broadcasts a real-time update to a character's vitals.
    /// </summary>
    /// <param name="patch">The delta update.</param>
    Task ReceiveCharacterUpdate(CharacterUpdateDto patch);

    /// <summary>
    /// Broadcasts the current initiative tracker state.
    /// </summary>
    /// <param name="entries">The full ordered list of initiative participants.</param>
    Task ReceiveInitiativeUpdate(IEnumerable<InitiativeEntryDto> entries);

    /// <summary>
    /// Broadcasts the current presence list to all connected clients.
    /// </summary>
    /// <param name="players">The list of all currently active players.</param>
    Task ReceivePresenceUpdate(IEnumerable<PlayerPresenceDto> players);

    /// <summary>
    /// Broadcasts changes to the chronicle state (scene changes, etc.).
    /// </summary>
    /// <param name="patch">The delta update.</param>
    Task ReceiveChronicleUpdate(ChronicleUpdateDto patch);
}
