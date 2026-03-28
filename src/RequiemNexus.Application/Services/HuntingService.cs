using System.Text.Json;
using System.Text.Json.Serialization;
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
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Resolves predator-type hunting pools, rolls with optional territory bonus, gains Vitae, and writes a ledger row.
/// </summary>
public sealed class HuntingService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    ITraitResolver traitResolver,
    IDiceService diceService,
    IVitaeService vitaeService,
    ISessionService sessionService,
    ILogger<HuntingService> logger) : IHuntingService
{
    private static readonly JsonSerializerOptions _poolJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly ITraitResolver _traitResolver = traitResolver;
    private readonly IDiceService _diceService = diceService;
    private readonly IVitaeService _vitaeService = vitaeService;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ILogger<HuntingService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<HuntResult>> ExecuteHuntAsync(
        int characterId,
        string userId,
        int? territoryId = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "hunt");

        Character? character = await _dbContext.Characters
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<HuntResult>.Failure("Character not found.");
        }

        if (character.PredatorType is not PredatorType predatorType)
        {
            return Result<HuntResult>.Failure("Predator Type not set.");
        }

        HuntingPoolDefinition? definition = await _dbContext.HuntingPoolDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.PredatorType == predatorType, cancellationToken);

        if (definition is null)
        {
            return Result<HuntResult>.Failure("Hunting pool definition not found.");
        }

        PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(definition.PoolDefinitionJson, _poolJsonOptions);
        if (pool is null)
        {
            return Result<HuntResult>.Failure("Invalid hunting pool configuration.");
        }

        int resolvedDice = await _traitResolver.ResolvePoolAsync(character, pool);
        bool territoryBonusApplied = false;
        int territoryRating = 0;

        if (territoryId is int tid)
        {
            if (character.CampaignId is null)
            {
                return Result<HuntResult>.Failure("Territory does not belong to this campaign.");
            }

            FeedingTerritory? territory = await _dbContext.FeedingTerritories
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tid, cancellationToken);

            if (territory is null)
            {
                return Result<HuntResult>.Failure("Territory not found.");
            }

            if (territory.CampaignId != character.CampaignId)
            {
                return Result<HuntResult>.Failure("Territory does not belong to this campaign.");
            }

            territoryBonusApplied = true;
            territoryRating = territory.Rating;
            resolvedDice += territoryRating;
        }

        int poolSize = resolvedDice < 1 ? 1 : resolvedDice;
        RollResult roll = _diceService.Roll(poolSize, tenAgain: true);

        int mechanicalVitae =
            definition.BaseVitaeGain + (roll.Successes * definition.PerSuccessVitaeGain);

        int vitaeBefore = character.CurrentVitae;

        if (mechanicalVitae > 0)
        {
            Result<int> gain = await _vitaeService.GainVitaeAsync(
                characterId,
                userId,
                mechanicalVitae,
                "Hunt",
                cancellationToken);

            if (!gain.IsSuccess)
            {
                return Result<HuntResult>.Failure(gain.Error ?? "Could not gain Vitae.");
            }
        }

        int vitaeAfter = await _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => c.CurrentVitae)
            .FirstAsync(cancellationToken);

        int vitaeGained = Math.Max(0, vitaeAfter - vitaeBefore);

        ResonanceOutcome resonance = HuntResonanceMapper.FromSuccesses(roll.Successes);

        string traitLabels = FormatPoolTraitLabels(pool);
        string territoryLine = territoryBonusApplied ? $" (+{territoryRating} territory bonus)" : string.Empty;
        string poolDescription = $"{predatorType}: {traitLabels}, pool {poolSize} dice{territoryLine}";

        _dbContext.HuntingRecords.Add(
            new HuntingRecord
            {
                CharacterId = characterId,
                TerritoryId = territoryId,
                PredatorType = predatorType,
                PoolDescription = poolDescription,
                Successes = roll.Successes,
                VitaeGained = vitaeGained,
                Resonance = resonance,
                HuntedAt = DateTime.UtcNow,
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (character.CampaignId is int chronicleId)
        {
            try
            {
                await _sessionService.PublishDiceRollAsync(
                    userId,
                    chronicleId,
                    characterId,
                    poolDescription,
                    roll);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Dice feed publish failed for hunt (character {CharacterId}, chronicle {ChronicleId}).",
                    characterId,
                    chronicleId);
            }
        }
        else
        {
            _logger.LogInformation(
                "Skipping dice feed for hunt: character {CharacterId} has no campaign.",
                characterId);
        }

        _logger.LogInformation(
            "Hunt executed {@HuntEvent}",
            new
            {
                CharacterId = characterId,
                character.CampaignId,
                PredatorType = predatorType,
                PoolSize = poolSize,
                roll.Successes,
                VitaeGained = vitaeGained,
                Resonance = resonance,
                TerritoryId = territoryId,
            });

        return Result<HuntResult>.Success(
            new HuntResult(
                roll.Successes,
                vitaeGained,
                resonance,
                poolDescription,
                definition.NarrativeDescription,
                territoryBonusApplied));
    }

    private static string FormatPoolTraitLabels(PoolDefinition pool)
    {
        var parts = new List<string>();
        foreach (TraitReference trait in pool.Traits)
        {
            parts.Add(FormatTraitReference(trait));
        }

        return string.Join(" + ", parts);
    }

    private static string FormatTraitReference(TraitReference trait)
    {
        return trait.Type switch
        {
            TraitType.Attribute when trait.AttributeId.HasValue =>
                TraitMetadata.GetDisplayName(trait.AttributeId.Value),
            TraitType.Skill when trait.SkillId.HasValue =>
                TraitMetadata.GetDisplayName(trait.SkillId.Value),
            TraitType.Discipline when trait.DisciplineId.HasValue =>
                $"Discipline #{trait.DisciplineId.Value}",
            _ => trait.Type.ToString(),
        };
    }
}
