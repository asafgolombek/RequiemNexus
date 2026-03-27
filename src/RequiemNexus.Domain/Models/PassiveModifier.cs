using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Models;

/// <summary>
/// Value object representing a passive bonus or penalty from a power (Coil, Devotion, Covenant benefit).
/// Modifiers are never applied permanently; derived values are computed on demand.
/// </summary>
/// <param name="Target">The stat or trait being modified.</param>
/// <param name="Value">The numeric delta (positive or negative). Ignored for RuleBreaking type.</param>
/// <param name="ModifierType">How the modifier applies (Static, Conditional, RuleBreaking).</param>
/// <param name="Condition">Human-readable description of when a Conditional modifier applies.</param>
/// <param name="Source">Identifies the entity that generated this modifier.</param>
public record PassiveModifier(
    ModifierTarget Target,
    int Value,
    ModifierType ModifierType,
    string? Condition,
    ModifierSource Source)
{
    /// <summary>
    /// When <see cref="ModifierTarget.SkillPool"/> is used, the bonus applies only if the resolved pool includes this skill.
    /// Phase 11: equipped general items and services grant skill-targeted dice bonuses.
    /// </summary>
    public global::RequiemNexus.Domain.Enums.SkillId? AppliesToSkill { get; init; }
}
