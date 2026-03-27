using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Models;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Contested frenzy saves: Resolve + Blood Potency, optional Willpower-for-one-die, server-side roll, Beast tilt guard.
/// </summary>
public sealed class FrenzyService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    ITraitResolver traitResolver,
    IDiceService diceService,
    IWillpowerService willpowerService,
    ISessionService sessionService,
    ILogger<FrenzyService> logger) : IFrenzyService
{
    private static readonly PoolDefinition _frenzyResolvePool = new(
    [
        new TraitReference(TraitType.Attribute, AttributeId.Resolve, null, null),
    ]);

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly ITraitResolver _traitResolver = traitResolver;
    private readonly IDiceService _diceService = diceService;
    private readonly IWillpowerService _willpowerService = willpowerService;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ILogger<FrenzyService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<FrenzySaveResult>> RollFrenzySaveAsync(
        int characterId,
        string userId,
        FrenzyTrigger trigger,
        bool spendWillpower,
        CancellationToken cancellationToken = default)
    {
        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "roll frenzy save");

        Character? character = await _dbContext.Characters
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<FrenzySaveResult>.Failure("Character not found.");
        }

        bool beastActive = await _dbContext.CharacterTilts
            .AnyAsync(
                t => t.CharacterId == characterId
                    && t.IsActive
                    && (t.TiltType == TiltType.Frenzy || t.TiltType == TiltType.Rotschreck),
                cancellationToken);

        if (beastActive)
        {
            return Result<FrenzySaveResult>.Success(
                new FrenzySaveResult(0, false, false, trigger, null, SuppressedDueToBeastAlreadyActive: true));
        }

        int resolveDice = await _traitResolver.ResolvePoolAsync(character, _frenzyResolvePool);
        int poolSize = resolveDice + character.BloodPotency;
        bool willpowerSpent = false;

        if (spendWillpower && character.CurrentWillpower > 0)
        {
            poolSize = Math.Max(0, poolSize - 1);
            Result<int> wp = await _willpowerService.SpendWillpowerAsync(characterId, userId, 1, cancellationToken);
            if (!wp.IsSuccess)
            {
                return Result<FrenzySaveResult>.Failure(wp.Error ?? "Could not spend Willpower.");
            }

            willpowerSpent = true;
            character = await _dbContext.Characters
                .Include(c => c.Attributes)
                .Include(c => c.Skills)
                .FirstAsync(c => c.Id == characterId, cancellationToken);
        }

        if (poolSize <= 0)
        {
            poolSize = 1;
        }

        RollResult roll = _diceService.Roll(poolSize, tenAgain: true);
        bool saved = roll.Successes > 0;
        TiltType? tiltApplied = null;

        if (!saved)
        {
            tiltApplied = trigger == FrenzyTrigger.Rotschreck ? TiltType.Rotschreck : TiltType.Frenzy;
            var tilt = new CharacterTilt
            {
                CharacterId = characterId,
                TiltType = tiltApplied.Value,
                AppliedAt = DateTime.UtcNow,
                IsActive = true,
                AppliedByUserId = userId,
            };

            _dbContext.CharacterTilts.Add(tilt);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _sessionService.BroadcastCharacterUpdateAsync(characterId);
        }

        string poolDescription = $"Frenzy save ({trigger}): Resolve + Blood Potency, pool {poolSize} dice";
        if (character.CampaignId is int chronicleId)
        {
            try
            {
                await _sessionService.PublishDiceRollAsync(userId, chronicleId, characterId, poolDescription, roll);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Dice feed publish failed for frenzy save (character {CharacterId}, chronicle {ChronicleId}).",
                    characterId,
                    chronicleId);
            }
        }
        else
        {
            _logger.LogInformation(
                "Skipping dice feed for frenzy save: character {CharacterId} has no campaign.",
                characterId);
        }

        return Result<FrenzySaveResult>.Success(
            new FrenzySaveResult(
                roll.Successes,
                saved,
                willpowerSpent,
                trigger,
                tiltApplied,
                SuppressedDueToBeastAlreadyActive: false));
    }
}
