namespace RequiemNexus.Data.RealTime;

/// <summary>
/// A delta representing real-time changes to the overall chronicle state.
/// </summary>
/// <param name="ChronicleId">Primary key of the chronicle being updated.</param>
/// <param name="ActiveScene">Name or description of the current scene.</param>
/// <param name="BeatAwardedMessage">Optional broadcast message for recent Beat awards.</param>
/// <param name="DegenerationCheckRequired">Optional ST alert for a pending degeneration roll.</param>
/// <param name="DegenerationCheckClearedCharacterId">When set, clients remove this character from pending degeneration UI.</param>
public record ChronicleUpdateDto(
    int ChronicleId,
    string? ActiveScene = null,
    string? BeatAwardedMessage = null,
    DegenerationCheckAlertDto? DegenerationCheckRequired = null,
    int? DegenerationCheckClearedCharacterId = null);
