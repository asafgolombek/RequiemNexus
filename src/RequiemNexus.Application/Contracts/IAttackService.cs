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
    /// <param name="weaponCharacterAssetId">
    /// When null, the attack is unarmed: no weapon damage dice are rolled (only net hits from the attack roll apply).
    /// When set, must reference the attacker's equipped, active <see cref="RequiemNexus.Data.Models.CharacterAsset"/> row
    /// whose catalog item is a <see cref="RequiemNexus.Data.Models.WeaponAsset"/> with <c>Damage &gt; 0</c>; dice count is taken from the weapon profile.
    /// </param>
    /// <param name="damageSource">Damage classification tag.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AttackResult> ResolveMeleeAttackAsync(
        string userId,
        int encounterId,
        int attackerCharacterId,
        int defenderDefense,
        PoolDefinition attackPool,
        int? weaponCharacterAssetId,
        DamageSource damageSource,
        CancellationToken cancellationToken = default);
}
