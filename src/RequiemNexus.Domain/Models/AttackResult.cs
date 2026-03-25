using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Models;

/// <summary>
/// Structured outcome of a melee attack resolution (dice only; persistence is Application-owned).
/// </summary>
/// <param name="AttackSuccesses">Uncapped successes on the attack roll.</param>
/// <param name="DefenseApplied">Defense subtracted from the attack (typically defender's Defense rating).</param>
/// <param name="NetAttackSuccesses">Successes after Defense, floored at zero.</param>
/// <param name="WeaponDamageSuccesses">Successes on the weapon damage dice pool.</param>
/// <param name="DamageSource">Tag for damage type and future tilt hooks.</param>
/// <param name="TotalDamageInstances">
/// Number of health-track applications to perform: net attack successes plus weapon damage successes (MVP aggregation).
/// </param>
public sealed record AttackResult(
    int AttackSuccesses,
    int DefenseApplied,
    int NetAttackSuccesses,
    int WeaponDamageSuccesses,
    DamageSource DamageSource,
    int TotalDamageInstances);
