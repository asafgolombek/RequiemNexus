namespace RequiemNexus.Domain.Models;

/// <summary>
/// One dice-pool penalty entry from an active Condition. A single condition type may map to several entries.
/// <see cref="PoolTarget"/> uses <see cref="ConditionPoolTarget"/> constants; application layer maps them to <see cref="Enums.ModifierTarget"/>.
/// </summary>
/// <param name="PoolTarget">Logical pool scope (e.g. <see cref="ConditionPoolTarget.AllPools"/>).</param>
/// <param name="Delta">Dice added (negative for penalties).</param>
/// <param name="IsNoActionFlag">Reserved for proactive-action suppression (UI); not consumed by the pool resolver today.</param>
public record ConditionPenaltyModifier(string PoolTarget, int Delta, bool IsNoActionFlag = false);
