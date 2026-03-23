namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Storyteller request to record a feeding that establishes or escalates a Blood Bond.
/// Exactly one of <see cref="RegnantCharacterId"/>, <see cref="RegnantNpcId"/>, or non-empty
/// <see cref="RegnantDisplayName"/> must be provided.
/// </summary>
/// <param name="ChronicleId">Campaign id (must match the thrall's chronicle).</param>
/// <param name="ThrallCharacterId">Character who drank.</param>
/// <param name="RegnantCharacterId">PC regnant, if applicable.</param>
/// <param name="RegnantNpcId">Chronicle NPC regnant, if applicable.</param>
/// <param name="RegnantDisplayName">Unlinked regnant name; trimmed for storage, normalized for <c>RegnantKey</c>.</param>
/// <param name="Notes">Optional ST notes on the bond row.</param>
public record RecordFeedingRequest(
    int ChronicleId,
    int ThrallCharacterId,
    int? RegnantCharacterId,
    int? RegnantNpcId,
    string? RegnantDisplayName,
    string? Notes);
