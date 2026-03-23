namespace RequiemNexus.Data.RealTime;

/// <summary>
/// Payload for session hub relationship broadcasts (Blood Bond, Predatory Aura, lineage, ghouls).
/// </summary>
/// <param name="UpdateType">Which relationship subsystem produced the update.</param>
/// <param name="AffectedCharacterId">Character whose state changed, if applicable.</param>
/// <param name="Summary">Human-readable description for UI toasts or logs.</param>
public record RelationshipUpdateDto(
    RelationshipUpdateType UpdateType,
    int? AffectedCharacterId,
    string Summary);
