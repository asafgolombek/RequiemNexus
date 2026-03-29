using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.Models;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Events;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public sealed class HumanityService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    IDomainEventDispatcher domainEventDispatcher,
    IDiceService diceService,
    ISessionService sessionService,
    IConditionService conditionService,
    ILogger<HumanityService> logger) : IHumanityService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;
    private readonly IDiceService _diceService = diceService;
    private readonly ISessionService _sessionService = sessionService;
    private readonly IConditionService _conditionService = conditionService;
    private readonly ILogger<HumanityService> _logger = logger;

    /// <inheritdoc />
    public int GetEffectiveMaxHumanity(Character character)
    {
        return 10 - character.GetDisciplineRating("Crúac");
    }

    /// <inheritdoc />
    public async Task EvaluateStainsAsync(int characterId, string userId)
    {
        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "evaluate Humanity stains");

        Character? character = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character == null)
        {
            throw new InvalidOperationException($"Character {characterId} not found.");
        }

        if (character.HumanityStains >= character.Humanity)
        {
            _domainEventDispatcher.Dispatch(
                new DegenerationCheckRequiredEvent(characterId, DegenerationReason.StainsThreshold));
        }
    }

    /// <inheritdoc />
    public async Task<Result<DegenerationRollOutcome>> ExecuteDegenerationRollAsync(
        int characterId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "roll degeneration");

        Character? character = await _dbContext.Characters
            .Include(c => c.Attributes)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<DegenerationRollOutcome>.Failure("Character not found.");
        }

        int poolDice = character.Humanity <= 0
            ? 0
            : character.GetAttributeRating(AttributeId.Resolve) + (7 - character.Humanity);

        RollResult roll = _diceService.Roll(poolDice, tenAgain: true);
        bool succeeded = roll.Successes >= 1;
        bool guiltyApplied = false;

        if (succeeded)
        {
            character.HumanityStains = 0;
        }
        else
        {
            character.Humanity = Math.Max(0, character.Humanity - 1);
            character.HumanityStains = 0;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!succeeded && roll.IsDramaticFailure)
        {
            await _conditionService.ApplyConditionAsync(
                characterId,
                ConditionType.Guilty,
                customName: null,
                descriptionOverride: null,
                userId);
            guiltyApplied = true;
        }

        string poolLabel = character.Humanity <= 0
            ? "Degeneration (chance die at Humanity 0)"
            : $"Degeneration: Resolve + (7 − Humanity), pool {poolDice} dice";

        if (character.CampaignId is int chronicleId)
        {
            try
            {
                await _sessionService.PublishDiceRollAsync(userId, chronicleId, characterId, poolLabel, roll);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Dice feed publish failed for degeneration roll (character {CharacterId}, chronicle {ChronicleId}).",
                    characterId,
                    chronicleId);
            }
        }
        else
        {
            _logger.LogInformation(
                "Skipping dice feed for degeneration: character {CharacterId} has no campaign.",
                characterId);
        }

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);

        await EvaluateStainsAsync(characterId, userId);

        _logger.LogInformation(
            "Degeneration roll for character {CharacterId}: successes={Successes}, humanityNow={Humanity}, guilty={Guilty}",
            characterId,
            roll.Successes,
            character.Humanity,
            guiltyApplied);

        return Result<DegenerationRollOutcome>.Success(
            new DegenerationRollOutcome(
                roll.Successes,
                succeeded,
                character.Humanity,
                guiltyApplied));
    }
}
