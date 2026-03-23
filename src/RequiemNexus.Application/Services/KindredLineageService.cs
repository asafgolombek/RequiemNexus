using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Observability;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application orchestration for Kindred lineage (sire links) and Blood Sympathy dice rolls.
/// </summary>
public class KindredLineageService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    ITraitResolver traitResolver,
    IDiceService diceService,
    RelationshipWebMetrics relationshipWebMetrics,
    ILogger<KindredLineageService> logger) : IKindredLineageService
{
    private const int _maxSireChainDepth = 10;

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
    private readonly ILogger<KindredLineageService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<Unit>> SetSireCharacterAsync(int characterId, int sireCharacterId, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        Character? subject = await db.Characters.FirstOrDefaultAsync(c => c.Id == characterId);
        if (subject == null)
        {
            return Result<Unit>.Failure($"Character {characterId} was not found.");
        }

        if (!subject.CampaignId.HasValue)
        {
            return Result<Unit>.Failure("Character is not attached to a chronicle.");
        }

        int campaignId = subject.CampaignId.Value;
        await _authHelper.RequireStorytellerAsync(campaignId, userId, "set Kindred lineage");

        Result<Unit> validation = await ValidatePcSireAssignmentAsync(db, characterId, sireCharacterId);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        subject.SireCharacterId = sireCharacterId;
        subject.SireNpcId = null;
        subject.SireDisplayName = null;
        await db.SaveChangesAsync();

        _relationshipWebMetrics.RecordLineageMutation("set_pc_sire");
        _logger.LogInformation(
            "Lineage: set PC sire {SireCharacterId} on character {CharacterId} in campaign {CampaignId} {CorrelationId}",
            sireCharacterId,
            characterId,
            campaignId,
            correlationId);

        return Result<Unit>.Success(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> SetSireNpcAsync(int characterId, int sireNpcId, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        Character? subject = await db.Characters.FirstOrDefaultAsync(c => c.Id == characterId);
        if (subject == null)
        {
            return Result<Unit>.Failure($"Character {characterId} was not found.");
        }

        if (!subject.CampaignId.HasValue)
        {
            return Result<Unit>.Failure("Character is not attached to a chronicle.");
        }

        int campaignId = subject.CampaignId.Value;
        await _authHelper.RequireStorytellerAsync(campaignId, userId, "set Kindred lineage");

        ChronicleNpc? npc = await db.ChronicleNpcs.AsNoTracking().FirstOrDefaultAsync(n => n.Id == sireNpcId);
        if (npc == null)
        {
            return Result<Unit>.Failure($"Chronicle NPC {sireNpcId} was not found.");
        }

        if (npc.CampaignId != campaignId)
        {
            return Result<Unit>.Failure("The NPC sire must belong to the same chronicle as the character.");
        }

        subject.SireNpcId = sireNpcId;
        subject.SireCharacterId = null;
        subject.SireDisplayName = null;
        await db.SaveChangesAsync();

        _relationshipWebMetrics.RecordLineageMutation("set_npc_sire");
        _logger.LogInformation(
            "Lineage: set NPC sire {SireNpcId} on character {CharacterId} in campaign {CampaignId} {CorrelationId}",
            sireNpcId,
            characterId,
            campaignId,
            correlationId);

        return Result<Unit>.Success(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> SetSireDisplayNameAsync(int characterId, string? name, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        Character? subject = await db.Characters.FirstOrDefaultAsync(c => c.Id == characterId);
        if (subject == null)
        {
            return Result<Unit>.Failure($"Character {characterId} was not found.");
        }

        if (!subject.CampaignId.HasValue)
        {
            return Result<Unit>.Failure("Character is not attached to a chronicle.");
        }

        await _authHelper.RequireStorytellerAsync(subject.CampaignId.Value, userId, "set Kindred lineage");

        string? normalized = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        subject.SireCharacterId = null;
        subject.SireNpcId = null;
        subject.SireDisplayName = normalized;
        await db.SaveChangesAsync();

        _relationshipWebMetrics.RecordLineageMutation("set_sire_display_name");
        _logger.LogInformation(
            "Lineage: set external sire display name on character {CharacterId} in campaign {CampaignId} {CorrelationId}",
            characterId,
            subject.CampaignId.Value,
            correlationId);

        return Result<Unit>.Success(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> ClearSireAsync(int characterId, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        Character? subject = await db.Characters.FirstOrDefaultAsync(c => c.Id == characterId);
        if (subject == null)
        {
            return Result<Unit>.Failure($"Character {characterId} was not found.");
        }

        if (!subject.CampaignId.HasValue)
        {
            return Result<Unit>.Failure("Character is not attached to a chronicle.");
        }

        await _authHelper.RequireStorytellerAsync(subject.CampaignId.Value, userId, "clear Kindred lineage");

        subject.SireCharacterId = null;
        subject.SireNpcId = null;
        subject.SireDisplayName = null;
        await db.SaveChangesAsync();

        _relationshipWebMetrics.RecordLineageMutation("clear_sire");
        _logger.LogInformation(
            "Lineage: cleared sire on character {CharacterId} in campaign {CampaignId} {CorrelationId}",
            characterId,
            subject.CampaignId.Value,
            correlationId);

        return Result<Unit>.Success(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<LineageGraphDto> GetLineageGraphAsync(int characterId, string userId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        Character? character = await db.Characters
            .AsNoTracking()
            .Include(c => c.SireCharacter)
            .Include(c => c.SireNpc)
            .Include(c => c.Childer)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} was not found.");

        if (!character.CampaignId.HasValue)
        {
            throw new InvalidOperationException("Character is not attached to a chronicle.");
        }

        int campaignId = character.CampaignId.Value;
        await _authHelper.RequireCampaignMemberAsync(db, campaignId, userId, "view Kindred lineage");

        int bp = character.BloodPotency;
        int bsr = BloodSympathyRules.ComputeRating(bp);
        KinNodeDto? sireNode = BuildSireNode(character);
        List<KinNodeDto> childer = character.Childer
            .OrderBy(ch => ch.Name)
            .Select(ch => new KinNodeDto(
                ch.Id,
                null,
                ch.Name,
                ch.BloodPotency,
                BloodSympathyRules.ComputeRating(ch.BloodPotency),
                DegreeOfSeparation: 1))
            .ToList();

        return new LineageGraphDto(
            character.Id,
            character.Name,
            bp,
            bsr,
            sireNode,
            childer);
    }

    /// <inheritdoc />
    public async Task<Result<RollResult>> RollBloodSympathyAsync(int characterId, int targetCharacterId, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
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

        return Result<RollResult>.Success(roll);
    }

    private static KinNodeDto? BuildSireNode(Character character)
    {
        if (character.SireCharacter != null)
        {
            Character s = character.SireCharacter;
            return new KinNodeDto(
                s.Id,
                null,
                s.Name,
                s.BloodPotency,
                BloodSympathyRules.ComputeRating(s.BloodPotency),
                DegreeOfSeparation: 1);
        }

        if (character.SireNpc != null)
        {
            return new KinNodeDto(
                null,
                character.SireNpc.Id,
                character.SireNpc.Name,
                null,
                null,
                DegreeOfSeparation: 1);
        }

        if (!string.IsNullOrWhiteSpace(character.SireDisplayName))
        {
            return new KinNodeDto(
                null,
                null,
                character.SireDisplayName.Trim(),
                null,
                null,
                DegreeOfSeparation: 1);
        }

        return null;
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

    private IDisposable BeginCorrelationScope(string correlationId) =>
        _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })
        ?? NoOpDisposable.Instance;

    private async Task<Result<Unit>> ValidatePcSireAssignmentAsync(
        ApplicationDbContext db,
        int subjectCharacterId,
        int sireCharacterId)
    {
        if (subjectCharacterId == sireCharacterId)
        {
            return Result<Unit>.Failure("A character cannot be their own sire.");
        }

        Character? sire = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == sireCharacterId);
        if (sire == null)
        {
            return Result<Unit>.Failure($"Sire character {sireCharacterId} was not found.");
        }

        Character? subject = await db.Characters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == subjectCharacterId);
        if (subject == null)
        {
            return Result<Unit>.Failure($"Character {subjectCharacterId} was not found.");
        }

        if (!subject.CampaignId.HasValue || subject.CampaignId != sire.CampaignId)
        {
            return Result<Unit>.Failure("The sire must belong to the same chronicle as the character.");
        }

        int? walker = sireCharacterId;
        for (int depth = 0; depth < _maxSireChainDepth && walker.HasValue; depth++)
        {
            if (walker.Value == subjectCharacterId)
            {
                return Result<Unit>.Failure("Assigning this sire would create a lineage cycle.");
            }

            walker = await db.Characters.AsNoTracking()
                .Where(c => c.Id == walker.Value)
                .Select(c => c.SireCharacterId)
                .FirstOrDefaultAsync();
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
