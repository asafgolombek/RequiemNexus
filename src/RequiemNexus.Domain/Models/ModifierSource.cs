using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Models;

/// <summary>
/// Identifies the source of a passive modifier for traceability and debugging.
/// </summary>
/// <param name="SourceType">The type of entity that generated the modifier.</param>
/// <param name="SourceId">The ID of the source entity (e.g., CoilDefinition.Id).</param>
public record ModifierSource(ModifierSourceType SourceType, int SourceId);
