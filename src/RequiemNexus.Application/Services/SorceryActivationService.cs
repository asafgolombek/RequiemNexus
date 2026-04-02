using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Events;
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
    IDomainEventDispatcher domainEventDispatcher,
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
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;
    private readonly ILogger<SorceryActivationService> _logger = logger;

    /// <inheritdoc />
    public async Task<BeginRiteActivationResult> BeginRiteActivationAsync(
        int characterId,
        int characterRiteId,
        string userId,
        BeginRiteActivationRequest request)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "begin rite activation");

        Character? character = await _dbContext.Characters
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
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

        if (def.SorceryType == SorceryType.Theban && character.Humanity < def.Level)
        {
            throw new InvalidOperationException(
                $"Theban Sorcery requires Humanity {def.Level} or higher to cast this miracle (character has Humanity {character.Humanity}).");
        }

        int traditionDots = GetTraditionDisciplineDots(character, def.SorceryType);
        if (traditionDots < def.Level)
        {
            throw new InvalidOperationException(
                $"Insufficient {def.SorceryType} dots to cast this rite (need {def.Level}, have {traditionDots}).");
        }

        if (def.RequiresElder && character.BloodPotency < SorceryElderRules.MinimumBloodPotency)
        {
            throw new InvalidOperationException(
                $"This rite requires Blood Potency {SorceryElderRules.MinimumBloodPotency} or higher (elder-ranked miracle). Character has Blood Potency {character.BloodPotency}.");
        }

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

        if (request.ExtraVitae != 0 && def.SorceryType != SorceryType.Cruac)
        {
            throw new InvalidOperationException("Extra Vitae may only be spent on Crúac rituals.");
        }

        int extraVitae = def.SorceryType == SorceryType.Cruac ? Math.Clamp(request.ExtraVitae, 0, 5) : 0;

        (int vitaeCost, int wpCost, int stainGain) = RiteRequirementValidator.AggregateInternalCosts(requirements);

        var resources = new RiteActivationResourceSnapshot(
            character.CurrentVitae,
            character.CurrentWillpower,
            character.HumanityStains);

        Result<bool> resOk = RiteRequirementValidator.ValidateResources(requirements, resources);
        if (!resOk.IsSuccess)
        {
            throw new InvalidOperationException(resOk.Error);
        }

        int totalVitaeSpend = vitaeCost + extraVitae;
        if (character.CurrentVitae < totalVitaeSpend)
        {
            throw new InvalidOperationException(
                $"Insufficient Vitae. This rite requires {totalVitaeSpend} Vitae (including optional bonus Vitae).");
        }

        if (totalVitaeSpend > 0 || wpCost > 0 || stainGain > 0)
        {
            await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx =
                await _dbContext.Database.BeginTransactionAsync();

            if (totalVitaeSpend > 0)
            {
                Result<int> vitaeResult = await _vitaeService.SpendVitaeAsync(
                    characterId,
                    userId,
                    totalVitaeSpend,
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

        if (def.SorceryType == SorceryType.Necromancy && character.Humanity >= 7)
        {
            _domainEventDispatcher.Dispatch(
                new DegenerationCheckRequiredEvent(characterId, DegenerationReason.NecromancyActivation));
        }

        _logger.LogInformation(
            "Rite activation costs applied: Character {CharacterId}, CharacterRite {CharacterRiteId}, Rite {RiteName}, Vitae {VitaeCost} (extra {ExtraVitae}), Willpower {WillpowerCost}, Stains {StainGain}, Requirements {RequirementSummary}",
            characterId,
            characterRiteId,
            def.Name,
            totalVitaeSpend,
            extraVitae,
            wpCost,
            stainGain,
            string.Join(';', requirements.Select(r => $"{r.Type}:{r.Value}")));

        character = await _dbContext.Characters
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .FirstAsync(c => c.Id == characterId);

        int unmodifiedPoolSize = ResolveRiteTraitPool(character, def);

        int dicePool = unmodifiedPoolSize + extraVitae;

        int sympathyDice = await TryResolveRitualBloodSympathyBonusAsync(
            characterId,
            character,
            def.SorceryType,
            request.TargetCharacterId);

        dicePool += sympathyDice;

        if (sympathyDice > 0)
        {
            _logger.LogInformation(
                "Ritual Blood Sympathy bonus: Character {CharacterId}, Rite {RiteName}, BonusDice {BonusDice}",
                characterId,
                def.Name,
                sympathyDice);
        }

        int minutesPerRoll = traditionDots > def.Level ? 15 : 30;
        int maxExtendedRolls = Math.Max(0, unmodifiedPoolSize);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
        return new BeginRiteActivationResult(
            dicePool,
            maxExtendedRolls,
            def.TargetSuccesses,
            minutesPerRoll,
            traditionDots);
    }

    /// <summary>
    /// Resolves the ritual dice pool from the rite pool definition only (no extra Vitae or Blood Sympathy).
    /// This matches the unmodified dice pool cap on extended rolls (V:tR 2e p. 152).
    /// </summary>
    private int ResolveRiteTraitPool(Character character, SorceryRiteDefinition def)
    {
        if (string.IsNullOrEmpty(def.PoolDefinitionJson))
        {
            return 0;
        }

        try
        {
            PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(def.PoolDefinitionJson, _jsonOptions);
            return pool != null ? _traitResolver.ResolvePool(character, pool) : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to resolve pool after activation for rite {RiteId} on character {CharacterId}",
                def.Id,
                character.Id);
            return 0;
        }
    }

    /// <summary>
    /// Applies V:tR 2e p. 153 Blood Sympathy dice to ritual pools when a valid in-chronicle Kindred target is named.
    /// </summary>
    private async Task<int> TryResolveRitualBloodSympathyBonusAsync(
        int ritualistCharacterId,
        Character ritualist,
        SorceryType tradition,
        int? targetCharacterId)
    {
        if (targetCharacterId is not int tid || tid == ritualistCharacterId)
        {
            return 0;
        }

        if (tradition is not (SorceryType.Cruac or SorceryType.Theban or SorceryType.Necromancy))
        {
            return 0;
        }

        if (!ritualist.CampaignId.HasValue)
        {
            throw new InvalidOperationException(
                "Blood Sympathy ritual targeting requires the ritualist to belong to a chronicle.");
        }

        Character? target = await _dbContext.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == tid);
        if (target == null)
        {
            throw new InvalidOperationException($"Ritual target character {tid} was not found.");
        }

        if (target.CampaignId != ritualist.CampaignId)
        {
            throw new InvalidOperationException("Ritual target must belong to the same chronicle as the ritualist.");
        }

        if (target.CreatureType != CreatureType.Vampire)
        {
            throw new InvalidOperationException("Blood Sympathy ritual bonuses apply only when the target is a vampire.");
        }

        IReadOnlyDictionary<int, int?> sireMap =
            await KindredLineageSireMapBuilder.BuildForCampaignAsync(_dbContext, ritualist.CampaignId.Value);

        int? degree = KindredLineageDegree.TryGetShortestDegree(ritualistCharacterId, tid, sireMap);
        if (degree is null or < 1)
        {
            return 0;
        }

        int r1 = BloodSympathyRules.ComputeRating(ritualist.BloodPotency);
        int r2 = BloodSympathyRules.ComputeRating(target.BloodPotency);
        int maxRange = BloodSympathyRules.EffectiveRange(r1, r2);
        if (degree.Value > maxRange)
        {
            return 0;
        }

        int baseBonus = BloodSympathyRules.RitualSympathyBonusThebanOrNecromancy(degree.Value);
        return tradition == SorceryType.Cruac ? baseBonus * 2 : baseBonus;
    }

    private int GetTraditionDisciplineDots(Character character, SorceryType type) =>
        type switch
        {
            SorceryType.Cruac => character.GetDisciplineRating("Crúac"),
            SorceryType.Theban => character.GetDisciplineRating("Theban Sorcery"),
            SorceryType.Necromancy => character.GetDisciplineRating("Necromancy"),
            _ => 0,
        };
}
