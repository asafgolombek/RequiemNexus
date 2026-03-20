using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for Conditions and Tilts.
/// Verifies that the requesting user is the character's owner or the campaign Storyteller before mutating.
/// Automatically writes Beat ledger entries when a Beat-awarding Condition is resolved.
/// Uses <see cref="IDbContextFactory{TContext}"/> so Blazor Server circuits can run overlapping loads without
/// sharing one scoped <see cref="ApplicationDbContext"/> instance (avoids concurrent access exceptions).
/// </summary>
public class ConditionService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IConditionRules conditionRules,
    IBeatLedgerService beatLedger,
    ILogger<ConditionService> logger,
    IAuthorizationHelper authHelper,
    ICharacterCreationRules creationRules,
    ISessionService sessionService) : IConditionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IConditionRules _conditionRules = conditionRules;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly ILogger<ConditionService> _logger = logger;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ICharacterCreationRules _creationRules = creationRules;

    /// <inheritdoc />
    public async Task<CharacterCondition> ApplyConditionAsync(
        int characterId,
        ConditionType type,
        string? customName,
        string? descriptionOverride,
        string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "apply or resolve conditions and tilts");

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        CharacterCondition condition = new()
        {
            CharacterId = characterId,
            ConditionType = type,
            CustomName = customName,
            Description = descriptionOverride,
            AppliedAt = DateTime.UtcNow,
            AwardsBeat = _conditionRules.AwardsBeatOnResolve(type),
            AppliedByUserId = userId,
        };

        db.CharacterConditions.Add(condition);
        await db.SaveChangesAsync();

        await sessionService.BroadcastCharacterUpdateAsync(characterId);

        string? ownerId = await db.Characters.AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => c.ApplicationUserId)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(ownerId))
        {
            await sessionService.NotifyConditionToastAsync(
                ownerId,
                new ConditionNotificationDto(
                    characterId,
                    condition.CustomName ?? condition.ConditionType.ToString(),
                    IsTilt: false,
                    IsRemoval: false));
        }

        _logger.LogInformation(
            "Condition {ConditionType} applied to character {CharacterId} by user {UserId}",
            type,
            characterId,
            userId);

        return condition;
    }

    /// <inheritdoc />
    public async Task ResolveConditionAsync(int conditionId, string userId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        CharacterCondition condition = await db.CharacterConditions
            .Include(c => c.Character)
            .FirstOrDefaultAsync(c => c.Id == conditionId)
            ?? throw new InvalidOperationException($"Condition {conditionId} not found.");

        if (condition.IsResolved)
        {
            throw new InvalidOperationException($"Condition {conditionId} is already resolved.");
        }

        await _authHelper.RequireCharacterAccessAsync(condition.CharacterId, userId, "apply or resolve conditions and tilts");

        condition.IsResolved = true;
        condition.ResolvedAt = DateTime.UtcNow;

        if (condition.AwardsBeat)
        {
            await _beatLedger.RecordBeatAsync(
                condition.CharacterId,
                condition.Character?.CampaignId,
                BeatSource.ConditionResolved,
                $"Resolved Condition: {condition.CustomName ?? condition.ConditionType.ToString()}",
                userId);

            Character character = await db.Characters.FindAsync(condition.CharacterId)
                ?? throw new InvalidOperationException($"Character {condition.CharacterId} not found.");

            character.Beats++;

            if (_creationRules.TryConvertBeats(character.Beats, out int newBeats, out int xpGained))
            {
                character.Beats = newBeats;
                character.ExperiencePoints += xpGained;
                character.TotalExperiencePoints += xpGained;

                await _beatLedger.RecordXpCreditAsync(
                    character.Id,
                    character.CampaignId,
                    xpGained,
                    XpSource.BeatConversion,
                    $"Converted 5 Beats to {xpGained} XP",
                    null);
            }
        }

        await db.SaveChangesAsync();

        await sessionService.BroadcastCharacterUpdateAsync(condition.CharacterId);

        string? ownerResolved = await db.Characters.AsNoTracking()
            .Where(c => c.Id == condition.CharacterId)
            .Select(c => c.ApplicationUserId)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(ownerResolved))
        {
            await sessionService.NotifyConditionToastAsync(
                ownerResolved,
                new ConditionNotificationDto(
                    condition.CharacterId,
                    condition.CustomName ?? condition.ConditionType.ToString(),
                    IsTilt: false,
                    IsRemoval: true));
        }

        _logger.LogInformation(
            "Condition {ConditionId} ({ConditionType}) resolved for character {CharacterId} by user {UserId}. BeatAwarded={AwardsBeat}",
            conditionId,
            condition.ConditionType,
            condition.CharacterId,
            userId,
            condition.AwardsBeat);
    }

    /// <inheritdoc />
    public async Task<List<CharacterCondition>> GetConditionsAsync(int characterId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        return await db.CharacterConditions
            .Where(c => c.CharacterId == characterId)
            .OrderByDescending(c => c.AppliedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CharacterTilt> ApplyTiltAsync(
        int characterId,
        TiltType type,
        string? customName,
        int? encounterId,
        string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "apply or resolve conditions and tilts");

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        CharacterTilt tilt = new()
        {
            CharacterId = characterId,
            TiltType = type,
            CustomName = customName,
            EncounterId = encounterId,
            AppliedAt = DateTime.UtcNow,
            IsActive = true,
            AppliedByUserId = userId,
        };

        db.CharacterTilts.Add(tilt);
        await db.SaveChangesAsync();

        await sessionService.BroadcastCharacterUpdateAsync(characterId);

        string? ownerTilt = await db.Characters.AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => c.ApplicationUserId)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(ownerTilt))
        {
            await sessionService.NotifyConditionToastAsync(
                ownerTilt,
                new ConditionNotificationDto(
                    characterId,
                    tilt.CustomName ?? tilt.TiltType.ToString(),
                    IsTilt: true,
                    IsRemoval: false));
        }

        _logger.LogInformation(
            "Tilt {TiltType} applied to character {CharacterId} by user {UserId}",
            type,
            characterId,
            userId);

        return tilt;
    }

    /// <inheritdoc />
    public async Task RemoveTiltAsync(int tiltId, string userId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        CharacterTilt tilt = await db.CharacterTilts
            .FirstOrDefaultAsync(t => t.Id == tiltId)
            ?? throw new InvalidOperationException($"Tilt {tiltId} not found.");

        if (!tilt.IsActive)
        {
            throw new InvalidOperationException($"Tilt {tiltId} is already inactive.");
        }

        await _authHelper.RequireCharacterAccessAsync(tilt.CharacterId, userId, "apply or resolve conditions and tilts");

        tilt.IsActive = false;
        tilt.RemovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await sessionService.BroadcastCharacterUpdateAsync(tilt.CharacterId);

        string? ownerRm = await db.Characters.AsNoTracking()
            .Where(c => c.Id == tilt.CharacterId)
            .Select(c => c.ApplicationUserId)
            .FirstOrDefaultAsync();
        if (!string.IsNullOrEmpty(ownerRm))
        {
            await sessionService.NotifyConditionToastAsync(
                ownerRm,
                new ConditionNotificationDto(
                    tilt.CharacterId,
                    tilt.CustomName ?? tilt.TiltType.ToString(),
                    IsTilt: true,
                    IsRemoval: true));
        }

        _logger.LogInformation(
            "Tilt {TiltId} ({TiltType}) removed from character {CharacterId} by user {UserId}",
            tiltId,
            tilt.TiltType,
            tilt.CharacterId,
            userId);
    }

    /// <inheritdoc />
    public async Task<List<CharacterTilt>> GetActiveTiltsAsync(int characterId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        return await db.CharacterTilts
            .Where(t => t.CharacterId == characterId && t.IsActive)
            .OrderBy(t => t.AppliedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}
