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
/// Torpor entry, awakening, and starvation milestone notifications for vampires.
/// </summary>
public sealed class TorporService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    IVitaeService vitaeService,
    ISessionService sessionService,
    ILogger<TorporService> logger) : ITorporService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly IVitaeService _vitaeService = vitaeService;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ILogger<TorporService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<Unit>> EnterTorporAsync(
        int characterId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<Unit>.Failure("Character not found.");
        }

        if (!character.CampaignId.HasValue)
        {
            return Result<Unit>.Failure("Character is not in a campaign.");
        }

        await _authorizationHelper.RequireStorytellerAsync(character.CampaignId.Value, userId, "place a character in torpor");

        List<CharacterTilt> beastTilts = await _dbContext.CharacterTilts
            .Where(t =>
                t.CharacterId == characterId
                && t.IsActive
                && (t.TiltType == TiltType.Frenzy || t.TiltType == TiltType.Rotschreck))
            .ToListAsync(cancellationToken);

        DateTime now = DateTime.UtcNow;
        foreach (CharacterTilt t in beastTilts)
        {
            t.IsActive = false;
            t.RemovedAt = now;
        }

        character.TorporSince = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _sessionService.BroadcastCharacterUpdateAsync(characterId);

        _logger.LogInformation("Character {CharacterId} entered torpor at {UtcNow}.", characterId, now);

        return Result<Unit>.Success(default);
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> AwakenFromTorporAsync(
        int characterId,
        string userId,
        bool narrativeAwakening,
        CancellationToken cancellationToken = default)
    {
        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<Unit>.Failure("Character not found.");
        }

        if (!character.TorporSince.HasValue)
        {
            return Result<Unit>.Failure("Character is not in torpor.");
        }

        if (!character.CampaignId.HasValue)
        {
            return Result<Unit>.Failure("Character is not in a campaign.");
        }

        await _authorizationHelper.RequireStorytellerAsync(character.CampaignId.Value, userId, "awaken a character from torpor");

        if (!narrativeAwakening)
        {
            if (character.CurrentVitae < 1)
            {
                return Result<Unit>.Failure("Not enough Vitae to awaken (requires 1).");
            }

            Result<int> spend = await _vitaeService.SpendVitaeAsync(
                characterId,
                userId,
                1,
                "Awaken from torpor",
                cancellationToken);

            if (!spend.IsSuccess)
            {
                return Result<Unit>.Failure(spend.Error ?? "Could not spend Vitae to awaken.");
            }
        }

        character = await _dbContext.Characters
            .FirstAsync(c => c.Id == characterId, cancellationToken);

        character.TorporSince = null;
        character.LastStarvationNotifiedAt = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _sessionService.BroadcastCharacterUpdateAsync(characterId);

        _logger.LogInformation(
            "Character {CharacterId} awakened from torpor (narrative={Narrative}).",
            characterId,
            narrativeAwakening);

        return Result<Unit>.Success(default);
    }

    /// <inheritdoc />
    public async Task CheckStarvationIntervalAsync(int characterId, CancellationToken cancellationToken = default)
    {
        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character?.TorporSince is not DateTime torporSince)
        {
            return;
        }

        int thresholdDays = TorporDurationTable.GetMinimumDays(character.BloodPotency);
        if (thresholdDays == int.MaxValue)
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        TimeSpan thresholdSpan = TimeSpan.FromDays(thresholdDays);
        if (now - torporSince < thresholdSpan)
        {
            return;
        }

        if (character.LastStarvationNotifiedAt is DateTime lastNotified
            && now - lastNotified < thresholdSpan)
        {
            return;
        }

        character.LastStarvationNotifiedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Torpor starvation milestone for character {CharacterId} ({Name}): BP {BloodPotency}, torpor since {TorporSince}, threshold {ThresholdDays} days. Storyteller should advance Hunger / Starvation frenzy when ready.",
            characterId,
            character.Name,
            character.BloodPotency,
            torporSince,
            thresholdDays);
    }
}
