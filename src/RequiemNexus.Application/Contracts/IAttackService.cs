using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Resolves a melee attack into dice outcomes (Phase 14 — Storyteller encounter flow).
/// </summary>
public interface IAttackService
{
    /// <summary>
    /// Verifies encounter + Storyteller access, resolves the attack pool with modifiers, rolls weapon damage, and returns a structured result.
    /// </summary>
    /// <param name="userId">Authenticated Storyteller user.</param>
    /// <param name="encounterId">Active combat encounter.</param>
    /// <param name="attackerCharacterId">Attacking player character in the encounter.</param>
    /// <param name="defenderDefense">Defense total already computed for the defender (PC or NPC).</param>
    /// <param name="attackPool">Pool definition (e.g. Strength + Weaponry).</param>
    /// <param name="weaponDamageDice">Size of the weapon damage dice pool (0 skips the roll).</param>
    /// <param name="damageSource">Damage classification tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AttackResult> ResolveMeleeAttackAsync(
        string userId,
        int encounterId,
        int attackerCharacterId,
        int defenderDefense,
        PoolDefinition attackPool,
        int weaponDamageDice,
        DamageSource damageSource,
        CancellationToken cancellationToken = default);
}
