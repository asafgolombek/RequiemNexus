namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Suggested combat values when adding a Danse Macabre chronicle NPC to an encounter.
/// </summary>
public sealed record ChronicleNpcEncounterPrepDto(
    string Name,
    int SuggestedInitiativeMod,
    int SuggestedHealthBoxes,
    string? LinkedStatBlockName,
    int SuggestedMaxWillpower,
    bool TracksVitae,
    int SuggestedMaxVitae);
