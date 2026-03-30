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

/// <inheritdoc />
public sealed class TouchstoneService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    IDiceService diceService,
    ISessionService sessionService,
    IConditionService conditionService,
    IHumanityService humanityService,
    ILogger<TouchstoneService> logger) : ITouchstoneService
{
    private const string _touchstoneMeritName = "Touchstone";

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly IDiceService _diceService = diceService;
    private readonly ISessionService _sessionService = sessionService;
    private readonly IConditionService _conditionService = conditionService;
    private readonly IHumanityService _humanityService = humanityService;
    private readonly ILogger<TouchstoneService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<DegenerationRollOutcome>> RollRemorseAsync(
        int characterId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "roll remorse");

        Character? character = await _dbContext.Characters
            .Include(c => c.Attributes)
            .Include(c => c.Merits)
            .ThenInclude(m => m.Merit)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<DegenerationRollOutcome>.Failure("Character not found.");
        }

        if (character.HumanityStains <= 0)
        {
            return Result<DegenerationRollOutcome>.Failure("No stains to roll remorse for");
        }

        if (character.HumanityStains >= character.Humanity)
        {
            return Result<DegenerationRollOutcome>.Failure("Use degeneration roll, not remorse");
        }

        bool hasTouchstoneAnchor = HasActiveTouchstoneAnchor(character);
        int poolDice = character.Humanity <= 0
            ? 0
            : character.Humanity + (hasTouchstoneAnchor ? 1 : 0);

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
            ? "Remorse (chance die at Humanity 0)"
            : $"Remorse: Humanity{(hasTouchstoneAnchor ? " + Touchstone" : string.Empty)}, pool {poolDice} dice";

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
                    "Dice feed publish failed for remorse roll (character {CharacterId}, chronicle {ChronicleId}).",
                    characterId,
                    chronicleId);
            }
        }
        else
        {
            _logger.LogInformation(
                "Skipping dice feed for remorse: character {CharacterId} has no campaign.",
                characterId);
        }

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);

        await _humanityService.EvaluateStainsAsync(characterId, userId);

        _logger.LogInformation(
            "Remorse roll for character {CharacterId}: successes={Successes}, humanityNow={Humanity}, guilty={Guilty}",
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

    private static bool HasActiveTouchstoneAnchor(Character character)
    {
        if (!string.IsNullOrWhiteSpace(character.Touchstone))
        {
            return true;
        }

        foreach (CharacterMerit cm in character.Merits)
        {
            if (cm.Rating > 0 && string.Equals(cm.Merit?.Name, _touchstoneMeritName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
