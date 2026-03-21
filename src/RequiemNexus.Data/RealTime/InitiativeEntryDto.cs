namespace RequiemNexus.Data.RealTime;

/// <summary>
/// An entry in the real-time initiative tracker.
/// </summary>
/// <param name="CharacterId">Primary key of the character (if applicable).</param>
/// <param name="Name">Display name of the participant (ST-facing; may match <see cref="DisplayName"/>).</param>
/// <param name="InitiativeValue">The value used for sorting the order.</param>
/// <param name="IsActiveTurn">True if it is currently this participant's turn.</param>
/// <param name="IsNpc">True if the participant is a Storyteller-controlled NPC.</param>
/// <param name="Order">1-based turn order position.</param>
/// <param name="HasActed">True after the participant has acted this round.</param>
/// <param name="CurrentRound">Encounter round number (duplicated on each row for simple client handling).</param>
/// <param name="DisplayName">Name shown to players (masked NPCs when unrevealed).</param>
/// <param name="IsRevealed">False when players should not see the true NPC name.</param>
/// <param name="EntryId">Database id of the initiative entry row.</param>
public record InitiativeEntryDto(
    int? CharacterId,
    string Name,
    int InitiativeValue,
    bool IsActiveTurn,
    bool IsNpc,
    int Order = 0,
    bool HasActed = false,
    int CurrentRound = 1,
    string? DisplayName = null,
    bool IsRevealed = true,
    int EntryId = 0);
