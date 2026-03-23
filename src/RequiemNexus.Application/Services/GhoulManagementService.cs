using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Observability;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

#pragma warning disable SA1201 // Order favors cohesive instance helpers with static parsers grouped at end
#pragma warning disable SA1204

/// <summary>
/// Application orchestration for ghoul retainers: feeding, aging alerts, Discipline access, and release.
/// </summary>
public class GhoulManagementService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    RelationshipWebMetrics relationshipWebMetrics,
    ISessionService sessionService,
    ILogger<GhoulManagementService> logger) : IGhoulManagementService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly RelationshipWebMetrics _relationshipWebMetrics = relationshipWebMetrics;
    private readonly ISessionService _sessionService = sessionService;

    /// <inheritdoc />
    public async Task<Result<GhoulDto>> CreateGhoulAsync(CreateGhoulRequest request, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<GhoulDto>.Failure("Ghoul name is required.");
        }

        Result<(int? CharId, int? NpcId, string? Display)> regnant = ValidateRegnantSelection(request);
        if (!regnant.IsSuccess)
        {
            return Result<GhoulDto>.Failure(regnant.Error!);
        }

        (int? regnantCharacterId, int? regnantNpcId, string? regnantDisplayName) = regnant.Value;

        await _authHelper.RequireStorytellerAsync(request.ChronicleId, userId, "create a ghoul");

        if (!await db.Campaigns.AsNoTracking().AnyAsync(c => c.Id == request.ChronicleId))
        {
            return Result<GhoulDto>.Failure($"Chronicle {request.ChronicleId} was not found.");
        }

        if (regnantCharacterId.HasValue)
        {
            Character? regnantPc = await db.Characters.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == regnantCharacterId.Value);
            if (regnantPc == null)
            {
                return Result<GhoulDto>.Failure($"Regnant character {regnantCharacterId.Value} was not found.");
            }

            if (regnantPc.CampaignId != request.ChronicleId)
            {
                return Result<GhoulDto>.Failure("The regnant PC must belong to the same chronicle.");
            }
        }

        if (regnantNpcId.HasValue)
        {
            ChronicleNpc? npc = await db.ChronicleNpcs.AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == regnantNpcId.Value);
            if (npc == null)
            {
                return Result<GhoulDto>.Failure($"Chronicle NPC {regnantNpcId.Value} was not found.");
            }

            if (npc.CampaignId != request.ChronicleId)
            {
                return Result<GhoulDto>.Failure("The regnant NPC must belong to the same chronicle.");
            }
        }

        var ghoul = new Ghoul
        {
            ChronicleId = request.ChronicleId,
            Name = request.Name.Trim(),
            RegnantCharacterId = regnantCharacterId,
            RegnantNpcId = regnantNpcId,
            RegnantDisplayName = regnantDisplayName,
            LastFedAt = null,
            VitaeInSystem = 0,
            ApparentAge = request.ApparentAge,
            ActualAge = request.ActualAge,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        db.Ghouls.Add(ghoul);
        await db.SaveChangesAsync();

        await PushGhoulUpdateAsync(ghoul, $"Ghoul \"{ghoul.Name}\" added.");
        _relationshipWebMetrics.RecordGhoulMutation("create");
        logger.LogInformation("Ghoul {GhoulId} created {CorrelationId}", ghoul.Id, correlationId);

        return Result<GhoulDto>.Success(await MapToDtoAsync(db, ghoul.Id, DateTime.UtcNow));
    }

    /// <inheritdoc />
    public async Task<Result<GhoulDto>> UpdateGhoulAsync(UpdateGhoulRequest request, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Ghoul? ghoul = await db.Ghouls.FirstOrDefaultAsync(g => g.Id == request.GhoulId);
        if (ghoul == null)
        {
            return Result<GhoulDto>.Failure($"Ghoul {request.GhoulId} was not found.");
        }

        await _authHelper.RequireStorytellerAsync(ghoul.ChronicleId, userId, "update a ghoul");

        if (ghoul.IsReleased)
        {
            return Result<GhoulDto>.Failure("Released ghouls cannot be updated.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<GhoulDto>.Failure("Ghoul name is required.");
        }

        ghoul.Name = request.Name.Trim();
        ghoul.ApparentAge = request.ApparentAge;
        ghoul.ActualAge = request.ActualAge;
        ghoul.Notes = request.Notes;
        await db.SaveChangesAsync();

        await PushGhoulUpdateAsync(ghoul, $"Ghoul \"{ghoul.Name}\" updated.");
        _relationshipWebMetrics.RecordGhoulMutation("update");
        logger.LogInformation("Ghoul {GhoulId} updated {CorrelationId}", ghoul.Id, correlationId);

        return Result<GhoulDto>.Success(await MapToDtoAsync(db, ghoul.Id, DateTime.UtcNow));
    }

    /// <inheritdoc />
    public async Task<Result<GhoulDto>> FeedGhoulAsync(int ghoulId, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Ghoul? ghoul = await db.Ghouls.FirstOrDefaultAsync(g => g.Id == ghoulId);
        if (ghoul == null)
        {
            return Result<GhoulDto>.Failure($"Ghoul {ghoulId} was not found.");
        }

        await _authHelper.RequireStorytellerAsync(ghoul.ChronicleId, userId, "feed a ghoul");

        if (ghoul.IsReleased)
        {
            return Result<GhoulDto>.Failure("Released ghouls cannot be fed.");
        }

        DateTime utcNow = DateTime.UtcNow;
        ghoul.LastFedAt = utcNow;
        ghoul.VitaeInSystem = 1;
        await db.SaveChangesAsync();

        await PushGhoulUpdateAsync(ghoul, $"Ghoul \"{ghoul.Name}\" fed.");
        _relationshipWebMetrics.RecordGhoulMutation("feed");
        logger.LogInformation("Ghoul {GhoulId} fed {CorrelationId}", ghoulId, correlationId);

        return Result<GhoulDto>.Success(await MapToDtoAsync(db, ghoulId, utcNow));
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> ReleaseGhoulAsync(int ghoulId, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Ghoul? ghoul = await db.Ghouls.FirstOrDefaultAsync(g => g.Id == ghoulId);
        if (ghoul == null)
        {
            return Result<Unit>.Failure($"Ghoul {ghoulId} was not found.");
        }

        await _authHelper.RequireStorytellerAsync(ghoul.ChronicleId, userId, "release a ghoul");

        if (ghoul.IsReleased)
        {
            return Result<Unit>.Failure("This ghoul is already released.");
        }

        DateTime utcNow = DateTime.UtcNow;
        ghoul.IsReleased = true;
        ghoul.ReleasedAt = utcNow;
        await db.SaveChangesAsync();

        await PushGhoulUpdateAsync(ghoul, $"Ghoul \"{ghoul.Name}\" released.");
        _relationshipWebMetrics.RecordGhoulMutation("release");
        logger.LogInformation("Ghoul {GhoulId} released {CorrelationId}", ghoulId, correlationId);

        return Result<Unit>.Success(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<Result<Unit>> SetDisciplineAccessAsync(int ghoulId, IReadOnlyList<int> disciplineIds, string userId)
    {
        string correlationId = AmbientCorrelation.ForNewOperation();
        using IDisposable correlationScope = BeginCorrelationScope(correlationId);
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Ghoul? ghoul = await db.Ghouls.FirstOrDefaultAsync(g => g.Id == ghoulId);
        if (ghoul == null)
        {
            return Result<Unit>.Failure($"Ghoul {ghoulId} was not found.");
        }

        await _authHelper.RequireStorytellerAsync(ghoul.ChronicleId, userId, "set ghoul Discipline access");

        if (ghoul.IsReleased)
        {
            return Result<Unit>.Failure("Released ghouls cannot gain Discipline access.");
        }

        List<int> distinctOrdered = disciplineIds.Distinct().OrderBy(i => i).ToList();

        if (ghoul.RegnantCharacterId.HasValue)
        {
            Character? regnant = await db.Characters
                .Include(c => c.Clan)!.ThenInclude(cl => cl!.ClanDisciplines)
                .Include(c => c.Bloodlines).ThenInclude(b => b.BloodlineDefinition)
                .FirstOrDefaultAsync(c => c.Id == ghoul.RegnantCharacterId.Value);

            if (regnant == null)
            {
                return Result<Unit>.Failure($"Regnant character {ghoul.RegnantCharacterId.Value} was not found.");
            }

            if (distinctOrdered.Count > regnant.BloodPotency)
            {
                return Result<Unit>.Failure(
                    $"A ghoul may access at most {regnant.BloodPotency} in-clan Disciplines (regnant Blood Potency).");
            }

            foreach (int id in distinctOrdered)
            {
                if (!regnant.IsDisciplineInClan(id))
                {
                    return Result<Unit>.Failure($"Discipline {id} is not in-clan for the regnant.");
                }
            }
        }

        ghoul.AccessibleDisciplinesJson = distinctOrdered.Count == 0
            ? null
            : JsonSerializer.Serialize(distinctOrdered);
        await db.SaveChangesAsync();

        await PushGhoulUpdateAsync(ghoul, $"Ghoul \"{ghoul.Name}\" Discipline access updated.");
        _relationshipWebMetrics.RecordGhoulMutation("set_disciplines");
        logger.LogInformation("Ghoul {GhoulId} discipline access set {CorrelationId}", ghoulId, correlationId);

        return Result<Unit>.Success(Unit.Value);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GhoulDto>> GetGhoulsForChronicleAsync(int chronicleId, string userId)
    {
        await _authHelper.RequireStorytellerAsync(chronicleId, userId, "list ghouls");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        List<int> ids = await db.Ghouls.AsNoTracking()
            .Where(g => g.ChronicleId == chronicleId && !g.IsReleased)
            .OrderBy(g => g.Name)
            .Select(g => g.Id)
            .ToListAsync();

        List<GhoulDto> list = [];
        foreach (int id in ids)
        {
            list.Add(await MapToDtoAsync(db, id, now));
        }

        return list;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GhoulAgingAlertDto>> GetAgingAlertsAsync(int chronicleId, string userId)
    {
        await _authHelper.RequireStorytellerAsync(chronicleId, userId, "view ghoul aging alerts");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;

        List<Ghoul> overdue = await db.Ghouls.AsNoTracking()
            .Include(g => g.RegnantCharacter)
            .Include(g => g.RegnantNpc)
            .Where(g => g.ChronicleId == chronicleId && !g.IsReleased)
            .ToListAsync();

        var alerts = new List<GhoulAgingAlertDto>();
        foreach (Ghoul g in overdue)
        {
            if (!GhoulAgingRules.IsAgingDue(g.LastFedAt, now))
            {
                continue;
            }

            DateTime reference = g.LastFedAt ?? g.CreatedAt;
            int months = GhoulAgingRules.OverdueMonths(reference, now);
            alerts.Add(new GhoulAgingAlertDto(
                g.Id,
                g.Name,
                ResolveRegnantLabel(g),
                g.LastFedAt,
                months));
        }

        return alerts;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GhoulDto>> GetGhoulsForRegnantAsync(int regnantCharacterId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(regnantCharacterId, userId, "view ghouls");
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;

        Character? regnant = await db.Characters.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == regnantCharacterId);
        if (regnant?.CampaignId == null)
        {
            return [];
        }

        List<int> ids = await db.Ghouls.AsNoTracking()
            .Where(g => g.RegnantCharacterId == regnantCharacterId && !g.IsReleased)
            .OrderBy(g => g.Name)
            .Select(g => g.Id)
            .ToListAsync();

        List<GhoulDto> list = [];
        foreach (int id in ids)
        {
            list.Add(await MapToDtoAsync(db, id, now));
        }

        return list;
    }

    private static Result<(int? CharId, int? NpcId, string? Display)> ValidateRegnantSelection(CreateGhoulRequest request)
    {
        int flags = (request.RegnantCharacterId.HasValue ? 1 : 0)
                    + (request.RegnantNpcId.HasValue ? 1 : 0)
                    + (!string.IsNullOrWhiteSpace(request.RegnantDisplayName) ? 1 : 0);

        if (flags != 1)
        {
            return Result<(int?, int?, string?)>.Failure(
                "Specify exactly one regnant: a PC character, an NPC, or a display name.");
        }

        if (request.RegnantCharacterId.HasValue)
        {
            return Result<(int?, int?, string?)>.Success((request.RegnantCharacterId.Value, null, null));
        }

        if (request.RegnantNpcId.HasValue)
        {
            return Result<(int?, int?, string?)>.Success((null, request.RegnantNpcId.Value, null));
        }

        string trimmed = request.RegnantDisplayName!.Trim();
        return Result<(int?, int?, string?)>.Success((null, null, trimmed));
    }

    private async Task PushGhoulUpdateAsync(Ghoul ghoul, string summary)
    {
        if (!ghoul.RegnantCharacterId.HasValue)
        {
            return;
        }

        await _sessionService.BroadcastCharacterUpdateAsync(ghoul.RegnantCharacterId.Value);
        await _sessionService.BroadcastRelationshipUpdateAsync(
            ghoul.ChronicleId,
            new RelationshipUpdateDto(RelationshipUpdateType.Ghoul, ghoul.RegnantCharacterId, summary));
    }

    private async Task<GhoulDto> MapToDtoAsync(ApplicationDbContext db, int ghoulId, DateTime now)
    {
        Ghoul g = await db.Ghouls.AsNoTracking()
            .Include(x => x.RegnantCharacter)
            .Include(x => x.RegnantNpc)
            .FirstAsync(x => x.Id == ghoulId);

        IReadOnlyList<int> discIds = ParseDisciplineIds(g.AccessibleDisciplinesJson);
        IReadOnlyList<string> discNames = await ResolveDisciplineNamesAsync(db, discIds);

        bool disciplineRulesApply = g.RegnantCharacterId.HasValue;
        int maxCount = 0;
        List<int> allowed = [];

        if (disciplineRulesApply)
        {
            Character? r = await db.Characters.AsNoTracking()
                .Include(c => c.Clan)!.ThenInclude(cl => cl!.ClanDisciplines)
                .Include(c => c.Bloodlines).ThenInclude(b => b.BloodlineDefinition)
                .FirstOrDefaultAsync(c => c.Id == g.RegnantCharacterId!.Value);

            if (r != null)
            {
                maxCount = r.BloodPotency;
                var set = new HashSet<int>();
                foreach (ClanDiscipline cd in r.Clan?.ClanDisciplines ?? [])
                {
                    set.Add(cd.DisciplineId);
                }

                CharacterBloodline? activeBl = r.Bloodlines.FirstOrDefault(b => b.Status == BloodlineStatus.Active);
                if (activeBl?.BloodlineDefinition != null)
                {
                    set.Add(activeBl.BloodlineDefinition.FourthDisciplineId);
                }

                allowed = set.OrderBy(x => x).ToList();
            }
        }

        bool agingDue = GhoulAgingRules.IsAgingDue(g.LastFedAt, now);
        DateTime agingReference = g.LastFedAt ?? g.CreatedAt;
        int overdueMonths = agingDue ? GhoulAgingRules.OverdueMonths(agingReference, now) : 0;

        return new GhoulDto(
            g.Id,
            g.ChronicleId,
            g.Name,
            g.RegnantCharacterId,
            g.RegnantNpcId,
            ResolveRegnantLabel(g),
            g.LastFedAt,
            g.VitaeInSystem,
            g.ApparentAge,
            g.ActualAge,
            discIds,
            discNames,
            g.Notes,
            g.IsReleased,
            g.CreatedAt,
            agingDue,
            overdueMonths,
            disciplineRulesApply,
            disciplineRulesApply ? maxCount : 0,
            disciplineRulesApply ? allowed : []);
    }

    private static IReadOnlyList<int> ParseDisciplineIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            int[]? arr = JsonSerializer.Deserialize<int[]>(json);
            return arr ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<string>> ResolveDisciplineNamesAsync(
        ApplicationDbContext db,
        IReadOnlyList<int> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        Dictionary<int, string> map = await db.Disciplines.AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name);

        List<string> names = [];
        foreach (int id in ids)
        {
            names.Add(map.TryGetValue(id, out string? n) ? n : $"#{id}");
        }

        return names;
    }

    private static string ResolveRegnantLabel(Ghoul g) =>
        g.RegnantCharacter?.Name
        ?? g.RegnantNpc?.Name
        ?? g.RegnantDisplayName
        ?? string.Empty;

    private IDisposable BeginCorrelationScope(string correlationId) =>
        logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId })
            ?? NoOpDisposable.Instance;
}
