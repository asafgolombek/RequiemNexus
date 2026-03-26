using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Observability;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application orchestration for Kindred lineage (sire links).
/// Blood Sympathy rolls are handled by <see cref="BloodSympathyRollService"/>.
/// </summary>
public class KindredLineageService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    RelationshipWebMetrics relationshipWebMetrics,
    ILogger<KindredLineageService> logger) : IKindredLineageService
{
    private const int _maxSireChainDepth = 10;

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
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
