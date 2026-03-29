namespace RequiemNexus.Data.RealTime;

/// <summary>
/// Pushed to the chronicle SignalR group when a character must resolve a degeneration check.
/// </summary>
/// <param name="CharacterId">Character requiring the roll.</param>
/// <param name="CharacterName">Display name for ST banner copy.</param>
/// <param name="Humanity">Current Humanity rating (used in pool formula).</param>
/// <param name="ResolveRating">Current Resolve attribute (used in pool formula).</param>
public record DegenerationCheckAlertDto(
    int CharacterId,
    string CharacterName,
    int Humanity,
    int ResolveRating);
