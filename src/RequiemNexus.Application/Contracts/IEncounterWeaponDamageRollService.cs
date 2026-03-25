using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Lets a character owner roll melee weapon damage during an active encounter and broadcast the result to the chronicle session log.
/// </summary>
public interface IEncounterWeaponDamageRollService
{
    /// <summary>
    /// Validates ownership and encounter state, resolves weapon damage dice on the server, rolls, and publishes to the chronicle.
    /// </summary>
    /// <param name="userId">Authenticated user (must own <paramref name="characterId"/>).</param>
    /// <param name="chronicleId">Campaign id; must match the encounter's campaign.</param>
    /// <param name="encounterId">Active combat encounter.</param>
    /// <param name="characterId">Attacking PC.</param>
    /// <param name="weaponCharacterAssetId">Equipped weapon row, or null for unarmed (chance die only).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outcome for the caller UI.</returns>
    Task<EncounterWeaponDamageRollOutcomeDto> RollAndPublishAsync(
        string userId,
        int chronicleId,
        int encounterId,
        int characterId,
        int? weaponCharacterAssetId,
        CancellationToken cancellationToken = default);
}
