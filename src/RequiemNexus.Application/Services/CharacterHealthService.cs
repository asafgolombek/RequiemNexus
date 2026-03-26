using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Implements <see cref="ICharacterHealthService"/> for structured damage and Vitae healing on the health track.
/// </summary>
public class CharacterHealthService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    IVitaeService vitaeService,
    ISessionService sessionService,
    ILogger<CharacterHealthService> logger) : ICharacterHealthService
{
    /// <inheritdoc />
    public async Task ApplyStructuredDamageAsync(
        int characterId,
        string userId,
        HealthDamageKind kind,
        int instances,
        CancellationToken cancellationToken = default)
    {
        if (instances <= 0)
        {
            return;
        }

        await authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "apply health damage");

        Character? character = await dbContext.Characters
            .Include(c => c.Attributes)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        int max = character.CalculatedMaxHealth;
        string updated = HealthTrackMutator.ApplyDamage(character.HealthDamage, max, kind, instances);
        character.HealthDamage = updated;
        character.CurrentHealth = HealthTrackMutator.CountDamagedBoxes(updated, max);

        await dbContext.SaveChangesAsync(cancellationToken);
        await sessionService.BroadcastCharacterUpdateAsync(characterId);

        logger.LogInformation(
            "Applied {Instances}×{Kind} damage to character {CharacterId} (track length {MaxHealth}).",
            instances,
            kind,
            characterId,
            max);
    }

    /// <inheritdoc />
    public Task ApplyDamageFromAttackAsync(
        int characterId,
        string userId,
        AttackResult attackResult,
        CancellationToken cancellationToken = default)
    {
        HealthDamageKind kind = attackResult.DamageSource.ToHealthDamageKind();
        return ApplyStructuredDamageAsync(characterId, userId, kind, attackResult.TotalDamageInstances, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<int>> TryFastHealBashingWithVitaeAsync(
        int characterId,
        string userId,
        int boxCount,
        CancellationToken cancellationToken = default)
    {
        await authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "heal with Vitae");

        if (boxCount <= 0)
        {
            return Result<int>.Failure("Box count must be positive.");
        }

        Character? character = await dbContext.Characters
            .Include(c => c.Attributes)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<int>.Failure("Character not found.");
        }

        int max = character.CalculatedMaxHealth;
        string track = HealthTrackMutator.NormalizeTrack(character.HealthDamage, max);
        int bashingBoxes = track.Count(c => c == '/');
        int plannedBoxes = Math.Min(boxCount, bashingBoxes);

        if (plannedBoxes <= 0)
        {
            return Result<int>.Failure("No bashing damage to heal.");
        }

        Result<int> costResult = VitaeHealingCosts.TryGetVitaeCost(HealingReason.FastHealBashing, plannedBoxes);
        if (!costResult.IsSuccess)
        {
            return Result<int>.Failure(costResult.Error ?? "Invalid healing request.");
        }

        int vitaeNeeded = costResult.Value!;
        if (character.CurrentVitae < vitaeNeeded)
        {
            return Result<int>.Failure("Not enough Vitae.");
        }

        int healed = 0;

        for (int i = 0; i < plannedBoxes; i++)
        {
            track = HealthTrackMutator.HealRightmostBashing(track, max);
            healed++;
        }

        int vitaeSpent = healed * VitaeHealingCosts.VitaePerBashingBox;
        Result<int> spend = await vitaeService.SpendVitaeAsync(
            characterId,
            userId,
            vitaeSpent,
            "Fast heal bashing",
            cancellationToken);

        if (!spend.IsSuccess)
        {
            return Result<int>.Failure(spend.Error ?? "Could not spend Vitae.");
        }

        character.HealthDamage = track;
        character.CurrentHealth = HealthTrackMutator.CountDamagedBoxes(track, max);

        await dbContext.SaveChangesAsync(cancellationToken);
        await sessionService.BroadcastCharacterUpdateAsync(characterId);

        logger.LogInformation(
            "Healed {Healed} bashing box(es) on character {CharacterId} for {Vitae} Vitae.",
            healed,
            characterId,
            vitaeSpent);

        return Result<int>.Success(healed);
    }
}
