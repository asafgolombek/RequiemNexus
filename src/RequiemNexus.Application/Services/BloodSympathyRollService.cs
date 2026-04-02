using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Observability;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Rolls the Blood Sympathy dice pool for a character attempting to sense kin.
/// Extracted from <see cref="KindredLineageService"/> to keep each service focused.
/// </summary>
public class BloodSympathyRollService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    ITraitResolver traitResolver,
    IDiceService diceService,
    RelationshipWebMetrics relationshipWebMetrics,
    ISessionService sessionService,
    ILogger<BloodSympathyRollService> logger) : IBloodSympathyRollService
{
    private static readonly PoolDefinition _bloodSympathyPoolDefinition = new(
        new[]
        {
            new TraitReference(TraitType.Attribute, AttributeId.Wits, null, null),
            new TraitReference(TraitType.Skill, null, SkillId.Empathy, null),
        });

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ITraitResolver _traitResolver = traitResolver;
    private readonly IDiceService _diceService = diceService;
    private readonly RelationshipWebMetrics _relationshipWebMetrics = relationshipWebMetrics;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ILogger<BloodSympathyRollService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<RollResult>> RollBloodSympathyAsync(int characterId, int targetCharacterId, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        if (characterId == targetCharacterId)
        {
            return Result<RollResult>.Failure("A character cannot roll Blood Sympathy to sense themselves.");
        }

        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "roll Blood Sympathy");

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        Character? roller = await db.Characters
            .AsNoTracking()
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .FirstOrDefaultAsync(c => c.Id == characterId);

        Character? target = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == targetCharacterId);

        if (roller == null)
        {
            return Result<RollResult>.Failure($"Character {characterId} was not found.");
        }

        if (target == null)
        {
            return Result<RollResult>.Failure($"Character {targetCharacterId} was not found.");
        }

        if (!roller.CampaignId.HasValue
            || !target.CampaignId.HasValue
            || roller.CampaignId != target.CampaignId)
        {
            return Result<RollResult>.Failure("Both characters must belong to the same chronicle.");
        }

        int campaignId = roller.CampaignId.Value;
        IReadOnlyDictionary<int, int?> sireMap = await KindredLineageSireMapBuilder.BuildForCampaignAsync(db, campaignId);
        int? degree = KindredLineageDegree.TryGetShortestDegree(characterId, targetCharacterId, sireMap);
        if (degree is null)
        {
            return Result<RollResult>.Failure(
                "These characters are not connected by PC lineage in this chronicle, so Blood Sympathy does not apply.");
        }

        int ratingRoller = BloodSympathyRules.ComputeRating(roller.BloodPotency);
        int ratingTarget = BloodSympathyRules.ComputeRating(target.BloodPotency);
        int maxRange = BloodSympathyRules.EffectiveRange(ratingRoller, ratingTarget);
        if (degree.Value > maxRange)
        {
            return Result<RollResult>.Failure(
                "The target is beyond your effective Blood Sympathy range for this lineage.");
        }

        int traitPool = await _traitResolver.ResolvePoolAsync(roller, _bloodSympathyPoolDefinition);
        int diceCount = Math.Max(0, traitPool + ratingRoller);
        RollResult roll = _diceService.Roll(diceCount, tenAgain: true);

        _relationshipWebMetrics.RecordLineageMutation("blood_sympathy_roll");
        _logger.LogInformation(
            "Blood Sympathy roll: roller {RollerId} target {TargetId} campaign {CampaignId} degree {Degree} dice {Dice} successes {Successes} {CorrelationId}",
            characterId,
            targetCharacterId,
            campaignId,
            degree.Value,
            diceCount,
            roll.Successes,
            correlationId);

        string poolLabel =
            $"Blood Sympathy — Wits + Empathy + rating ({diceCount} dice) vs {target.Name ?? "kin"}";
        await _sessionService.PublishDiceRollAsync(userId, campaignId, characterId, poolLabel, roll);

        return Result<RollResult>.Success(roll);
    }
}
