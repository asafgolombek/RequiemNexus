using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Implements <see cref="IAttackService"/> for Storyteller-led melee attack resolution inside encounters.
/// </summary>
public class AttackService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    ICharacterService characterService,
    ITraitResolver traitResolver,
    IDiceService diceService,
    ILogger<AttackService> logger) : IAttackService
{
    /// <inheritdoc />
    public async Task<AttackResult> ResolveMeleeAttackAsync(
        string userId,
        int encounterId,
        int attackerCharacterId,
        int defenderDefense,
        PoolDefinition attackPool,
        int? weaponCharacterAssetId,
        DamageSource damageSource,
        CancellationToken cancellationToken = default)
    {
        CombatEncounter? encounter = await dbContext.CombatEncounters
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        await authorizationHelper.RequireStorytellerAsync(encounter.CampaignId, userId, "resolve combat attacks");

        bool attackerInEncounter = await dbContext.InitiativeEntries
            .AsNoTracking()
            .AnyAsync(
                e => e.EncounterId == encounterId && e.CharacterId == attackerCharacterId,
                cancellationToken);

        if (!attackerInEncounter)
        {
            throw new InvalidOperationException("The attacker is not part of this encounter.");
        }

        await authorizationHelper.RequireCharacterAccessAsync(attackerCharacterId, userId, "use attacker for encounter resolution");

        (Character attacker, _) = (await characterService.GetCharacterWithAccessCheckAsync(attackerCharacterId, userId))
            ?? throw new InvalidOperationException($"Character {attackerCharacterId} not found.");

        int attackPoolSize = await traitResolver.ResolvePoolAsync(attacker, attackPool);
        RollResult attackRoll = diceService.Roll(attackPoolSize);

        int weaponDamageDice = 0;
        if (weaponCharacterAssetId is int weaponRowId)
        {
            CharacterAsset? weaponRow = await dbContext.CharacterAssets
                .Include(ca => ca.Asset)
                .FirstOrDefaultAsync(ca => ca.Id == weaponRowId, cancellationToken);

            if (weaponRow == null)
            {
                throw new InvalidOperationException("Weapon inventory row not found.");
            }

            if (weaponRow.CharacterId != attackerCharacterId)
            {
                throw new InvalidOperationException("The selected weapon does not belong to the attacker.");
            }

            if (!weaponRow.IsEquipped || !CharacterAssetActiveHelper.IsEquippedAndActive(weaponRow))
            {
                throw new InvalidOperationException("The weapon must be equipped and functional to deal weapon damage.");
            }

            if (weaponRow.Asset is not WeaponAsset weaponProfile)
            {
                throw new InvalidOperationException("The selected inventory row is not a weapon.");
            }

            if (weaponProfile.Damage <= 0)
            {
                throw new InvalidOperationException(
                    "This weapon has no damage dice; use unarmed (no weapon selected) for attacks without a weapon pool.");
            }

            weaponDamageDice = weaponProfile.Damage;
        }

        int weaponSuccesses = 0;
        if (weaponDamageDice > 0)
        {
            RollResult weaponRoll = diceService.Roll(weaponDamageDice);
            weaponSuccesses = weaponRoll.Successes;
        }

        int cappedDefense = Math.Max(0, defenderDefense);
        int netHits = Math.Max(0, attackRoll.Successes - cappedDefense);
        int totalInstances = netHits + weaponSuccesses;

        logger.LogInformation(
            "Melee attack resolved for encounter {EncounterId}, attacker {AttackerId}: successes {Successes}, defense {Defense}, net {NetHits}, weapon {Weapon}, total damage instances {Total}.",
            encounterId,
            attackerCharacterId,
            attackRoll.Successes,
            cappedDefense,
            netHits,
            weaponSuccesses,
            totalInstances);

        return new AttackResult(
            attackRoll.Successes,
            cappedDefense,
            netHits,
            weaponSuccesses,
            damageSource,
            totalInstances);
    }
}
