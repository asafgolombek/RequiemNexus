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
/// Passive modifiers from active <see cref="CharacterCondition"/> rows and <see cref="IConditionRules"/> penalties.
/// </summary>
public sealed class ConditionModifierProvider(
    ApplicationDbContext dbContext,
    IConditionRules conditionRules,
    ILogger<ConditionModifierProvider> logger) : IModifierProvider
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IConditionRules _conditionRules = conditionRules;
    private readonly ILogger<ConditionModifierProvider> _logger = logger;

    /// <inheritdoc />
    public int Order => 10;

    /// <inheritdoc />
    public ModifierSourceType SourceType => ModifierSourceType.Condition;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PassiveModifier>> GetModifiersAsync(int characterId, CancellationToken cancellationToken = default)
    {
        List<CharacterCondition> activeConditions = await _dbContext.CharacterConditions
            .AsNoTracking()
            .Where(c => c.CharacterId == characterId && !c.IsResolved)
            .ToListAsync(cancellationToken);

        var modifiers = new List<PassiveModifier>();

        foreach (CharacterCondition row in activeConditions)
        {
            foreach (ConditionPenaltyModifier penalty in _conditionRules.GetPenalties(row.ConditionType))
            {
                if (penalty.Delta == 0)
                {
                    continue;
                }

                ModifierTarget? mapped = MapConditionPoolTargetToModifierTarget(penalty.PoolTarget);
                if (mapped is null)
                {
                    _logger.LogWarning(
                        "Unknown condition pool target {PoolTarget} for CharacterCondition {ConditionId} (character {CharacterId}).",
                        penalty.PoolTarget,
                        row.Id,
                        characterId);
                    continue;
                }

                string label = $"Condition ({row.ConditionType})";
                modifiers.Add(new PassiveModifier(
                    mapped.Value,
                    penalty.Delta,
                    ModifierType.Static,
                    label,
                    new ModifierSource(ModifierSourceType.Condition, row.Id)));
            }
        }

        return modifiers;
    }

    private static ModifierTarget? MapConditionPoolTargetToModifierTarget(string poolTarget) => poolTarget switch
    {
        ConditionPoolTarget.AllPools => ModifierTarget.AllDicePools,
        ConditionPoolTarget.PhysicalPools => ModifierTarget.PhysicalDicePools,
        ConditionPoolTarget.AllExceptFleeing => ModifierTarget.DicePoolsExceptFleeing,
        ConditionPoolTarget.ResolveComposure => ModifierTarget.PoolsUsingResolveOrComposure,
        ConditionPoolTarget.MentalPools => ModifierTarget.MentalDicePools,
        ConditionPoolTarget.Composure => ModifierTarget.PoolsUsingComposureAttribute,
        _ => null,
    };
}
