using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Handles rite activation pool resolution and cost application.
/// Separated from <see cref="SorceryService"/> (rite learning) to keep each class focused.
/// </summary>
public class SorceryActivationService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ISessionService sessionService,
    ITraitResolver traitResolver,
    IVitaeService vitaeService,
    IWillpowerService willpowerService,
    IHumanityService humanityService,
    ILogger<SorceryActivationService> logger) : ISorceryActivationService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ITraitResolver _traitResolver = traitResolver;
    private readonly IVitaeService _vitaeService = vitaeService;
    private readonly IWillpowerService _willpowerService = willpowerService;
    private readonly IHumanityService _humanityService = humanityService;
    private readonly ILogger<SorceryActivationService> _logger = logger;

    /// <inheritdoc />
    public async Task<int> ResolveRiteActivationPoolAsync(int characterId, int characterRiteId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "activate rite");

        Character? character = await _dbContext.Characters
            .Include(c => c.Disciplines)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Rites)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        CharacterRite? cr = character.Rites.FirstOrDefault(r => r.Id == characterRiteId && r.Status == RiteLearnStatus.Approved);
        if (cr == null)
        {
            cr = await _dbContext.CharacterRites
                .Include(r => r.SorceryRiteDefinition)
                .FirstOrDefaultAsync(r => r.Id == characterRiteId && r.CharacterId == characterId && r.Status == RiteLearnStatus.Approved);
        }

        if (cr?.SorceryRiteDefinition == null || string.IsNullOrEmpty(cr.SorceryRiteDefinition.PoolDefinitionJson))
        {
            return 0;
        }

        try
        {
            PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(cr.SorceryRiteDefinition.PoolDefinitionJson, _jsonOptions);
            return pool != null ? _traitResolver.ResolvePool(character, pool) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to resolve activation pool for rite {RiteId} ({RiteName}) on character {CharacterId}. PoolJson: {PoolJson}",
                cr.SorceryRiteDefinitionId,
                cr.SorceryRiteDefinition.Name,
                characterId,
                cr.SorceryRiteDefinition.PoolDefinitionJson);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<int> BeginRiteActivationAsync(
        int characterId,
        int characterRiteId,
        string userId,
        BeginRiteActivationRequest request)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "begin rite activation");

        Character? character = await _dbContext.Characters
            .Include(c => c.Disciplines)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Rites)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        CharacterRite? cr = character.Rites.FirstOrDefault(r => r.Id == characterRiteId && r.Status == RiteLearnStatus.Approved);
        if (cr == null)
        {
            cr = await _dbContext.CharacterRites
                .Include(r => r.SorceryRiteDefinition)
                .FirstOrDefaultAsync(r => r.Id == characterRiteId && r.CharacterId == characterId && r.Status == RiteLearnStatus.Approved);
        }

        if (cr?.SorceryRiteDefinition == null)
        {
            throw new InvalidOperationException("Approved character rite not found.");
        }

        SorceryRiteDefinition def = cr.SorceryRiteDefinition;
        Result<IReadOnlyList<RiteRequirement>> parsed = RiteRequirementValidator.ParseRequirements(def.RequirementsJson);
        if (!parsed.IsSuccess)
        {
            throw new InvalidOperationException(parsed.Error);
        }

        IReadOnlyList<RiteRequirement> requirements = parsed.Value ?? [];

        var ack = new RiteActivationAcknowledgment(
            request.AcknowledgePhysicalSacrament,
            request.AcknowledgeHeart,
            request.AcknowledgeMaterialOffering,
            request.AcknowledgeMaterialFocus);

        Result<bool> ackResult = RiteRequirementValidator.ValidateAcknowledgments(requirements, ack);
        if (!ackResult.IsSuccess)
        {
            throw new InvalidOperationException(ackResult.Error);
        }

        var resources = new RiteActivationResourceSnapshot(
            character.CurrentVitae,
            character.CurrentWillpower,
            character.HumanityStains);

        Result<bool> resOk = RiteRequirementValidator.ValidateResources(requirements, resources);
        if (!resOk.IsSuccess)
        {
            throw new InvalidOperationException(resOk.Error);
        }

        (int vitaeCost, int wpCost, int stainGain) = RiteRequirementValidator.AggregateInternalCosts(requirements);

        if (vitaeCost > 0 || wpCost > 0 || stainGain > 0)
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx =
                await _dbContext.Database.BeginTransactionAsync();

            if (vitaeCost > 0)
            {
                Result<int> vitaeResult = await _vitaeService.SpendVitaeAsync(
                    characterId,
                    userId,
                    vitaeCost,
                    $"Rite activation: {def.Name}");

                if (!vitaeResult.IsSuccess)
                {
                    throw new InvalidOperationException(
                        vitaeResult.Error ?? "Could not spend Vitae for rite activation.");
                }
            }

            if (wpCost > 0)
            {
                Result<int> wpResult = await _willpowerService.SpendWillpowerAsync(characterId, userId, wpCost);

                if (!wpResult.IsSuccess)
                {
                    throw new InvalidOperationException(
                        wpResult.Error ?? "Could not spend Willpower for rite activation.");
                }
            }

            if (stainGain > 0)
            {
                character.HumanityStains += stainGain;
            }

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            if (stainGain > 0)
            {
                await _humanityService.EvaluateStainsAsync(characterId, userId);
            }
        }

        _logger.LogInformation(
            "Rite activation costs applied: Character {CharacterId}, CharacterRite {CharacterRiteId}, Rite {RiteName}, Vitae {VitaeCost}, Willpower {WillpowerCost}, Stains {StainGain}, Requirements {RequirementSummary}",
            characterId,
            characterRiteId,
            def.Name,
            vitaeCost,
            wpCost,
            stainGain,
            string.Join(';', requirements.Select(r => $"{r.Type}:{r.Value}")));

        character = await _dbContext.Characters
            .Include(c => c.Disciplines)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .FirstAsync(c => c.Id == characterId);

        int poolSize = 0;
        if (!string.IsNullOrEmpty(def.PoolDefinitionJson))
        {
            try
            {
                PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(def.PoolDefinitionJson, _jsonOptions);
                poolSize = pool != null ? _traitResolver.ResolvePool(character, pool) : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resolve pool after activation for rite {RiteId} on character {CharacterId}",
                    def.Id,
                    characterId);
                poolSize = 0;
            }
        }

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
        return poolSize;
    }
}
