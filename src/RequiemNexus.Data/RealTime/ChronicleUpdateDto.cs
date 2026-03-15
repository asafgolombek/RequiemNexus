namespace RequiemNexus.Data.RealTime;

/// <summary>
/// A delta representing real-time changes to the overall chronicle state.
/// </summary>
/// <param name="ChronicleId">Primary key of the chronicle being updated.</param>
/// <param name="ActiveScene">Name or description of the current scene.</param>
/// <param name="BeatAwardedMessage">Optional broadcast message for recent Beat awards.</param>
public record ChronicleUpdateDto(
    int ChronicleId,
    string? ActiveScene = null,
    string? BeatAwardedMessage = null);
