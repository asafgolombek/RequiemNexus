using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Activates Discipline powers: enforces cost, resolves pools via <see cref="ITraitResolver"/>, and notifies session clients when resources change.
/// Dice rolls and feed publication are handled by the Blazor dice roller (same pattern as <see cref="SorceryActivationService"/>).
/// </summary>
public sealed class DisciplineActivationService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ITraitResolver traitResolver,
    IVitaeService vitaeService,
    IWillpowerService willpowerService,
    ISessionService sessionService,
    ILogger<DisciplineActivationService> logger) : IDisciplineActivationService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ITraitResolver _traitResolver = traitResolver;
    private readonly IVitaeService _vitaeService = vitaeService;
    private readonly IWillpowerService _willpowerService = willpowerService;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ILogger<DisciplineActivationService> _logger = logger;

    /// <inheritdoc />
    public async Task<int> ResolveActivationPoolAsync(int characterId, int disciplinePowerId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "resolve discipline pool");

        Character character = await LoadCharacterForActivationAsync(characterId);
        DisciplinePower power = await LoadDisciplinePowerAsync(disciplinePowerId);

        if (!CharacterHasPowerEligibility(character, power))
        {
            throw new InvalidOperationException("Character does not meet the required discipline rating for this power.");
        }

        if (string.IsNullOrEmpty(power.PoolDefinitionJson))
        {
            return 0;
        }

        try
        {
            PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(power.PoolDefinitionJson, _jsonOptions);
            return pool != null ? _traitResolver.ResolvePool(character, pool) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to resolve activation pool for discipline power {PowerId} ({PowerName}) on character {CharacterId}. PoolJson: {PoolJson}",
                power.Id,
                power.Name,
                characterId,
                power.PoolDefinitionJson);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<int> ActivatePowerAsync(int characterId, int disciplinePowerId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "activate discipline power");

        Character character = await LoadCharacterForActivationAsync(characterId);
        DisciplinePower power = await LoadDisciplinePowerAsync(disciplinePowerId);

        if (!CharacterHasPowerEligibility(character, power))
        {
            throw new InvalidOperationException("Character does not meet the required discipline rating for this power.");
        }

        if (string.IsNullOrEmpty(power.PoolDefinitionJson))
        {
            throw new InvalidOperationException("This power has no rollable pool.");
        }

        ActivationCost cost = ActivationCost.Parse(power.Cost);
        if (cost.IsNone && CostStringLooksNonTrivial(power.Cost))
        {
            _logger.LogWarning(
                "Discipline power {PowerId} ({PowerName}) has a non-empty cost string that did not parse to Vitae or Willpower: {CostString}",
                power.Id,
                power.Name,
                power.Cost);
        }

        if (!cost.IsNone)
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx =
                await _dbContext.Database.BeginTransactionAsync();

            if (cost.Type == ActivationCostType.Vitae)
            {
                Result<int> vitaeResult = await _vitaeService.SpendVitaeAsync(
                    characterId,
                    userId,
                    cost.Amount,
                    $"Discipline: {power.Name}");

                if (!vitaeResult.IsSuccess)
                {
                    throw new InvalidOperationException(vitaeResult.Error ?? "Could not spend Vitae for discipline activation.");
                }
            }
            else if (cost.Type == ActivationCostType.Willpower)
            {
                Result<int> wpResult = await _willpowerService.SpendWillpowerAsync(characterId, userId, cost.Amount);

                if (!wpResult.IsSuccess)
                {
                    throw new InvalidOperationException(wpResult.Error ?? "Could not spend Willpower for discipline activation.");
                }
            }

            await tx.CommitAsync();

            character = await LoadCharacterForActivationAsync(characterId);
        }

        int poolSize = 0;
        try
        {
            PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(power.PoolDefinitionJson, _jsonOptions);
            poolSize = pool != null ? await _traitResolver.ResolvePoolAsync(character, pool) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to resolve pool after activation for discipline power {PowerId} on character {CharacterId}",
                power.Id,
                characterId);
            poolSize = 0;
        }

        _logger.LogInformation(
            "Discipline power activated: Character {CharacterId}, Power {PowerId} ({PowerName}), CostType {CostType}, CostAmount {CostAmount}, PoolSize {PoolSize}",
            characterId,
            power.Id,
            power.Name,
            cost.Type,
            cost.Amount,
            poolSize);

        if (!cost.IsNone)
        {
            await _sessionService.BroadcastCharacterUpdateAsync(characterId);
        }

        return poolSize;
    }

    private async Task<Character> LoadCharacterForActivationAsync(int characterId)
    {
        Character? character = await _dbContext.Characters
            .Include(c => c.Disciplines)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == characterId);

        return character ?? throw new InvalidOperationException($"Character {characterId} not found.");
    }

    private async Task<DisciplinePower> LoadDisciplinePowerAsync(int disciplinePowerId)
    {
        DisciplinePower? power = await _dbContext.DisciplinePowers
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == disciplinePowerId);

        return power ?? throw new InvalidOperationException($"Discipline power {disciplinePowerId} not found.");
    }

    private bool CharacterHasPowerEligibility(Character character, DisciplinePower power)
    {
        int? rating = character.Disciplines.FirstOrDefault(d => d.DisciplineId == power.DisciplineId)?.Rating;
        return rating >= power.Level;
    }

    private bool CostStringLooksNonTrivial(string? costString)
    {
        if (string.IsNullOrWhiteSpace(costString))
        {
            return false;
        }

        var trimmed = costString.Trim().TrimStart('—', '-', '–');
        return !string.IsNullOrWhiteSpace(trimmed);
    }
}
