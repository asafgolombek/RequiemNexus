using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Pure rules for which <see cref="ConditionType"/> applies to a ritual roll outcome (V:tR 2e p. 152).
/// </summary>
public static class RiteRollOutcomeRules
{
    /// <summary>
    /// Returns the Condition to apply, or <c>null</c> when the rules define no Condition (e.g. Necromancy dramatic failure).
    /// </summary>
    public static ConditionType? TryResolveConditionType(SorceryType tradition, RiteRollOutcomeTrigger trigger) =>
        trigger switch
        {
            RiteRollOutcomeTrigger.ContinueAfterZeroSuccesses => ConditionType.Stumbled,
            RiteRollOutcomeTrigger.DramaticFailure => tradition switch
            {
                SorceryType.Cruac => ConditionType.Tempted,
                SorceryType.Theban => ConditionType.Humbled,
                _ => null,
            },
            RiteRollOutcomeTrigger.ExceptionalSuccess => tradition switch
            {
                SorceryType.Cruac => ConditionType.Ecstatic,
                SorceryType.Theban => ConditionType.Raptured,
                _ => null,
            },
            _ => null,
        };
}
