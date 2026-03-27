using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Models;

/// <summary>
/// A reference to a single trait (Attribute, Skill, or Discipline) that contributes to a dice pool.
/// Exactly one of AttributeId, SkillId, or DisciplineId is set depending on Type.
/// </summary>
/// <param name="Type">The type of trait.</param>
/// <param name="AttributeId">Set when Type is Attribute.</param>
/// <param name="SkillId">Set when Type is Skill.</param>
/// <param name="DisciplineId">Set when Type is Discipline.</param>
/// <param name="MinimumLevel">Optional minimum rating for Discipline type (e.g., Vigor 2).</param>
public record TraitReference(
    TraitType Type,
    AttributeId? AttributeId,
    SkillId? SkillId,
    int? DisciplineId,
    int? MinimumLevel = null);
