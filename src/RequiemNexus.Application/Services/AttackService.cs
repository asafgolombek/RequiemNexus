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
        int weaponDamageDice,
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
