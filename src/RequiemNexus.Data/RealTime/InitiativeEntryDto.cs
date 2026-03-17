namespace RequiemNexus.Data.RealTime;

/// <summary>
/// An entry in the real-time initiative tracker.
/// </summary>
/// <param name="CharacterId">Primary key of the character (if applicable).</param>
/// <param name="Name">Display name of the participant.</param>
/// <param name="InitiativeValue">The value used for sorting the order.</param>
/// <param name="IsActiveTurn">True if it is currently this participant's turn.</param>
/// <param name="IsNpc">True if the participant is a Storyteller-controlled NPC.</param>
public record InitiativeEntryDto(
    int? CharacterId,
    string Name,
    int InitiativeValue,
    bool IsActiveTurn,
    bool IsNpc);
