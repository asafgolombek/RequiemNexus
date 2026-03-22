namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Strongly-typed targets for passive modifiers. Used instead of freeform strings (Antigravity Rule #1).
/// </summary>
public enum ModifierTarget
{
    Defense,

    Speed,

    MaxHealth,

    Brawl,

    WoundPenalty,

    Athletics,

    Weaponry,

    Firearms,

    FirearmsAccuracy,

    /// <summary>
    /// Equipment or service bonus tied to a specific skill; see <c>PassiveModifier.AppliesToSkill</c>.
    /// </summary>
    SkillPool,
}
