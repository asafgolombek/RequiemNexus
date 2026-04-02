using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Applies blood sorcery ritual outcome Conditions per <see cref="RiteRollOutcomeRules"/>.
/// </summary>
public class RiteRollOutcomeService(
    IConditionService conditionService,
    ILogger<RiteRollOutcomeService> logger) : IRiteRollOutcomeService
{
    private readonly IConditionService _conditionService = conditionService;
    private readonly ILogger<RiteRollOutcomeService> _logger = logger;

    /// <inheritdoc />
    public async Task ApplyRiteRollOutcomeAsync(
        int characterId,
        string userId,
        SorceryType tradition,
        RiteRollOutcomeTrigger trigger,
        CancellationToken cancellationToken = default)
    {
        ConditionType? conditionType = RiteRollOutcomeRules.TryResolveConditionType(tradition, trigger);
        if (conditionType is null)
        {
            _logger.LogDebug(
                "No ritual outcome Condition for tradition {Tradition}, trigger {Trigger} on character {CharacterId}",
                tradition,
                trigger,
                characterId);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        await _conditionService.ApplyConditionAsync(
            characterId,
            conditionType.Value,
            customName: null,
            descriptionOverride: null,
            userId);

        _logger.LogInformation(
            "Rite roll outcome Condition {ConditionType} applied: Character {CharacterId}, Tradition {Tradition}, Trigger {Trigger}",
            conditionType.Value,
            characterId,
            tradition,
            trigger);
    }
}
