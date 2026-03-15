namespace RequiemNexus.Data.RealTime;

/// <summary>
/// A full snapshot of an active play session, used for initial hydration and reconnection.
/// </summary>
/// <param name="ChronicleId">Primary key of the chronicle.</param>
/// <param name="Presence">All currently connected players.</param>
/// <param name="RollHistory">The last N dice rolls for this session.</param>
/// <param name="Initiative">The current initiative tracker state.</param>
public record SessionStateDto(
    int ChronicleId,
    IEnumerable<PlayerPresenceDto> Presence,
    IEnumerable<DiceRollResultDto> RollHistory,
    IEnumerable<InitiativeEntryDto> Initiative);
