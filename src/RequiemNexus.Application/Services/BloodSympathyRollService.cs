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
        IReadOnlyDictionary<int, int?> sireMap = await BuildSireMapForCampaignAsync(db, campaignId);
        int? degree = TryGetLineageDegree(characterId, targetCharacterId, sireMap);
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

    private static async Task<IReadOnlyDictionary<int, int?>> BuildSireMapForCampaignAsync(
        ApplicationDbContext db,
        int campaignId)
    {
        var rows = await db.Characters
            .AsNoTracking()
            .Where(c => c.CampaignId == campaignId)
            .Select(c => new { c.Id, c.SireCharacterId })
            .ToListAsync();

        HashSet<int> idSet = rows.Select(r => r.Id).ToHashSet();
        return rows.ToDictionary(
            r => r.Id,
            r => r.SireCharacterId.HasValue && idSet.Contains(r.SireCharacterId.Value)
                ? r.SireCharacterId
                : null);
    }

    /// <summary>
    /// Shortest-path degree between two PCs using sire edges that stay inside the chronicle roster.
    /// </summary>
    /// <returns>Null when either id is missing from the map or there is no path.</returns>
    private static int? TryGetLineageDegree(
        int fromCharacterId,
        int toCharacterId,
        IReadOnlyDictionary<int, int?> sireByCharacterId)
    {
        if (!sireByCharacterId.ContainsKey(fromCharacterId) || !sireByCharacterId.ContainsKey(toCharacterId))
        {
            return null;
        }

        if (fromCharacterId == toCharacterId)
        {
            return 0;
        }

        var adjacency = new Dictionary<int, List<int>>();
        foreach ((int id, int? sireId) in sireByCharacterId)
        {
            if (!adjacency.ContainsKey(id))
            {
                adjacency[id] = [];
            }

            if (sireId.HasValue)
            {
                if (!adjacency.ContainsKey(sireId.Value))
                {
                    adjacency[sireId.Value] = [];
                }

                adjacency[id].Add(sireId.Value);
                adjacency[sireId.Value].Add(id);
            }
        }

        var queue = new Queue<(int Node, int Depth)>();
        var visited = new HashSet<int>();
        queue.Enqueue((fromCharacterId, 0));
        visited.Add(fromCharacterId);
        while (queue.Count > 0)
        {
            (int node, int depth) = queue.Dequeue();
            if (node == toCharacterId)
            {
                return depth;
            }

            if (!adjacency.TryGetValue(node, out List<int>? neighbors))
            {
                continue;
            }

            foreach (int n in neighbors)
            {
                if (visited.Add(n))
                {
                    queue.Enqueue((n, depth + 1));
                }
            }
        }

        return null;
    }
}
