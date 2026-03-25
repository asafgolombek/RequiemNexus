using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Implements <see cref="IEncounterWeaponDamageRollService"/> for player-owned weapon damage rolls in active encounters.
/// </summary>
public class EncounterWeaponDamageRollService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    IDiceService diceService,
    ISessionService sessionService,
    ILogger<EncounterWeaponDamageRollService> logger) : IEncounterWeaponDamageRollService
{
    private const int _maxPoolDescriptionLength = 100;

    /// <inheritdoc />
    public async Task<EncounterWeaponDamageRollOutcomeDto> RollAndPublishAsync(
        string userId,
        int chronicleId,
        int encounterId,
        int characterId,
        int? weaponCharacterAssetId,
        CancellationToken cancellationToken = default)
    {
        await authorizationHelper.RequireCharacterOwnerAsync(characterId, userId, "roll weapon damage in an encounter");

        CombatEncounter? encounter = await dbContext.CombatEncounters
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken)
            ?? throw new InvalidOperationException($"Encounter {encounterId} not found.");

        if (encounter.CampaignId != chronicleId)
        {
            throw new InvalidOperationException("This encounter does not belong to the selected chronicle.");
        }

        if (encounter.ResolvedAt != null || !encounter.IsActive || encounter.IsDraft)
        {
            throw new InvalidOperationException("Weapon damage can only be rolled during an active encounter.");
        }

        bool inEncounter = await dbContext.InitiativeEntries
            .AsNoTracking()
            .AnyAsync(
                e => e.EncounterId == encounterId && e.CharacterId == characterId,
                cancellationToken);

        if (!inEncounter)
        {
            throw new InvalidOperationException("Your character is not part of this encounter.");
        }

        int? charCampaignId = await dbContext.Characters
            .AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => c.CampaignId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!charCampaignId.HasValue || charCampaignId.Value != chronicleId)
        {
            throw new InvalidOperationException("This character is not in the selected chronicle.");
        }

        (int weaponDamageDice, string poolDescription) = await ResolveWeaponPoolAsync(
            characterId,
            weaponCharacterAssetId,
            cancellationToken);

        RollResult roll = diceService.Roll(weaponDamageDice);
        string description = TruncateDescription(poolDescription);

        await sessionService.PublishDiceRollAsync(userId, chronicleId, characterId, description, roll);

        logger.LogInformation(
            "Encounter weapon damage rolled: encounter {EncounterId}, character {CharacterId}, pool dice {Dice}, successes {Successes}, user {UserId}.",
            encounterId,
            characterId,
            weaponDamageDice,
            roll.Successes,
            userId);

        return new EncounterWeaponDamageRollOutcomeDto(
            roll.Successes,
            roll.IsExceptionalSuccess,
            roll.IsDramaticFailure,
            roll.DiceRolled.ToArray(),
            description);
    }

    private async Task<(int Dice, string Description)> ResolveWeaponPoolAsync(
        int characterId,
        int? weaponCharacterAssetId,
        CancellationToken cancellationToken)
    {
        if (weaponCharacterAssetId is not int weaponRowId)
        {
            return (0, "Melee weapon damage (unarmed)");
        }

        CharacterAsset? weaponRow = await dbContext.CharacterAssets
            .Include(ca => ca.Asset)
            .FirstOrDefaultAsync(ca => ca.Id == weaponRowId, cancellationToken);

        if (weaponRow == null)
        {
            throw new InvalidOperationException("Weapon inventory row not found.");
        }

        if (weaponRow.CharacterId != characterId)
        {
            throw new InvalidOperationException("The selected weapon does not belong to this character.");
        }

        if (!weaponRow.IsEquipped || !CharacterAssetActiveHelper.IsEquippedAndActive(weaponRow))
        {
            throw new InvalidOperationException("The weapon must be equipped and functional to roll weapon damage.");
        }

        if (weaponRow.Asset is not WeaponAsset weaponProfile)
        {
            throw new InvalidOperationException("The selected inventory row is not a weapon.");
        }

        if (weaponProfile.Damage <= 0)
        {
            throw new InvalidOperationException(
                "This weapon has no damage dice; choose unarmed or a different weapon.");
        }

        string name = string.IsNullOrWhiteSpace(weaponRow.Asset.Name) ? "weapon" : weaponRow.Asset.Name.Trim();
        return (weaponProfile.Damage, $"Melee weapon damage ({name})");
    }

    private string TruncateDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return "Melee weapon damage";
        }

        return description.Length <= _maxPoolDescriptionLength
            ? description
            : description[.._maxPoolDescriptionLength];
    }
}
