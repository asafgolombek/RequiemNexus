using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for blood sorcery rite learning and activation.
/// </summary>
public class SorceryService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    IBeatLedgerService beatLedger,
    ISessionService sessionService,
    ITraitResolver traitResolver,
    ILogger<SorceryService> logger) : ISorceryService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ITraitResolver _traitResolver = traitResolver;
    private readonly ILogger<SorceryService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<SorceryRiteSummaryDto>> GetEligibleRitesAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "view eligible rites");

        Character character = await _dbContext.Characters
            .AsNoTracking()
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
            .Include(c => c.Rites)
            .Include(c => c.Covenant)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CovenantId == null || character.CovenantJoinStatus != null)
        {
            return [];
        }

        int cruacRating = character.GetDisciplineRating("Crúac");
        int thebanRating = character.GetDisciplineRating("Theban Sorcery");

        var learnedOrPendingIds = character.Rites
            .Where(r => r.Status == RiteLearnStatus.Approved || r.Status == RiteLearnStatus.Pending)
            .Select(r => r.SorceryRiteDefinitionId)
            .ToHashSet();

        var query = await _dbContext.SorceryRiteDefinitions
            .AsNoTracking()
            .Include(s => s.RequiredCovenant)
            .Where(r => r.RequiredCovenantId == character.CovenantId
                && !learnedOrPendingIds.Contains(r.Id)
                && ((r.SorceryType == SorceryType.Cruac && cruacRating >= r.Level)
                    || (r.SorceryType == SorceryType.Theban && thebanRating >= r.Level)))
            .OrderBy(r => r.SorceryType)
            .ThenBy(r => r.Level)
            .ThenBy(r => r.Name)
            .ToListAsync();

        return query.Select(r => new SorceryRiteSummaryDto(r.Id, r.Name, r.Level, r.SorceryType, r.XpCost, r.RequiredCovenant?.Name ?? string.Empty)).ToList();
    }

    /// <inheritdoc />
    public async Task<CharacterRite> RequestLearnRiteAsync(int characterId, int sorceryRiteDefinitionId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "request rite learning");

        Character character = await _dbContext.Characters
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
            .Include(c => c.Rites)
            .Include(c => c.Covenant)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character must be in a campaign to learn rites.");
        }

        if (character.CovenantId == null || character.CovenantJoinStatus != null)
        {
            throw new InvalidOperationException("Character must be in an approved covenant to learn rites.");
        }

        SorceryRiteDefinition? rite = await _dbContext.SorceryRiteDefinitions
            .Include(r => r.RequiredCovenant)
            .FirstOrDefaultAsync(r => r.Id == sorceryRiteDefinitionId)
            ?? throw new InvalidOperationException($"Rite {sorceryRiteDefinitionId} not found.");

        if (rite.RequiredCovenantId != character.CovenantId)
        {
            throw new InvalidOperationException("Character's covenant does not match the rite's required covenant.");
        }

        int disciplineRating = rite.SorceryType == SorceryType.Cruac
            ? character.GetDisciplineRating("Crúac")
            : character.GetDisciplineRating("Theban Sorcery");

        if (disciplineRating < rite.Level)
        {
            throw new InvalidOperationException($"Character needs {rite.SorceryType} {rite.Level} to learn this rite.");
        }

        if (character.Rites.Any(r => r.SorceryRiteDefinitionId == sorceryRiteDefinitionId && r.Status == RiteLearnStatus.Pending))
        {
            throw new InvalidOperationException("A learning request for this rite is already pending.");
        }

        if (character.Rites.Any(r => r.SorceryRiteDefinitionId == sorceryRiteDefinitionId && r.Status == RiteLearnStatus.Approved))
        {
            throw new InvalidOperationException("Character has already learned this rite.");
        }

        if (character.ExperiencePoints < rite.XpCost)
        {
            throw new InvalidOperationException($"Insufficient XP. Requires {rite.XpCost}, character has {character.ExperiencePoints}.");
        }

        var cr = new CharacterRite
        {
            CharacterId = characterId,
            SorceryRiteDefinitionId = sorceryRiteDefinitionId,
            Status = RiteLearnStatus.Pending,
            AppliedAt = DateTime.UtcNow,
        };
        _dbContext.CharacterRites.Add(cr);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Rite learning requested: Character {CharacterId}, Rite {RiteId} ({RiteName})",
            characterId,
            sorceryRiteDefinitionId,
            rite.Name);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
        return cr;
    }

    /// <inheritdoc />
    public async Task<List<RiteApplicationDto>> GetPendingRiteApplicationsAsync(int campaignId, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "view pending rite applications");

        return await _dbContext.CharacterRites
            .AsNoTracking()
            .Include(cr => cr.Character)
            .Include(cr => cr.SorceryRiteDefinition)
            .Where(cr => cr.Status == RiteLearnStatus.Pending
                && cr.Character != null
                && cr.Character.CampaignId == campaignId)
            .OrderByDescending(cr => cr.AppliedAt)
            .Select(cr => new RiteApplicationDto(
                cr.Id,
                cr.CharacterId,
                cr.Character!.Name,
                cr.SorceryRiteDefinition!.Name,
                cr.SorceryRiteDefinition.SorceryType,
                cr.SorceryRiteDefinition.Level,
                cr.SorceryRiteDefinition.XpCost,
                cr.AppliedAt))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ApproveRiteLearnAsync(int characterRiteId, string? note, string storyTellerUserId)
    {
        CharacterRite? cr = await _dbContext.CharacterRites
            .Include(cr => cr.Character)
            .Include(cr => cr.SorceryRiteDefinition)
            .FirstOrDefaultAsync(cr => cr.Id == characterRiteId)
            ?? throw new InvalidOperationException($"Rite application {characterRiteId} not found.");

        if (cr.Character?.CampaignId == null)
        {
            throw new InvalidOperationException("Application is not associated with a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(cr.Character.CampaignId.Value, storyTellerUserId, "approve rite learning");

        if (cr.Status != RiteLearnStatus.Pending)
        {
            throw new InvalidOperationException("Only pending applications can be approved.");
        }

        var rite = cr.SorceryRiteDefinition!;
        var character = cr.Character!;

        int rowsAffected = await _dbContext.Characters
            .Where(c => c.Id == character.Id && c.ExperiencePoints >= rite.XpCost)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.ExperiencePoints, c => c.ExperiencePoints - rite.XpCost)
                .SetProperty(c => c.TotalExperiencePoints, c => c.TotalExperiencePoints - rite.XpCost));

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException(
                $"Character has insufficient XP for rite cost {rite.XpCost}, or a concurrent update occurred.");
        }

        await _beatLedger.RecordXpSpendAsync(
            character.Id,
            character.CampaignId,
            rite.XpCost,
            XpExpense.Rite,
            $"Learned rite: {rite.Name}",
            storyTellerUserId);

        cr.Status = RiteLearnStatus.Approved;
        cr.ResolvedAt = DateTime.UtcNow;
        cr.StorytellerNote = note;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Rite learning approved: CharacterRite {Id}, Character {CharacterId}, Rite {RiteName}",
            characterRiteId,
            cr.CharacterId,
            rite.Name);

        await _sessionService.BroadcastCharacterUpdateAsync(cr.CharacterId);
    }

    /// <inheritdoc />
    public async Task RejectRiteLearnAsync(int characterRiteId, string? note, string storyTellerUserId)
    {
        CharacterRite? cr = await _dbContext.CharacterRites
            .Include(cr => cr.Character)
            .FirstOrDefaultAsync(cr => cr.Id == characterRiteId)
            ?? throw new InvalidOperationException($"Rite application {characterRiteId} not found.");

        if (cr.Character?.CampaignId == null)
        {
            throw new InvalidOperationException("Application is not associated with a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(cr.Character.CampaignId.Value, storyTellerUserId, "reject rite learning");

        cr.Status = RiteLearnStatus.Rejected;
        cr.ResolvedAt = DateTime.UtcNow;
        cr.StorytellerNote = note;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Rite learning rejected: CharacterRite {Id}, Character {CharacterId}",
            characterRiteId,
            cr.CharacterId);

        await _sessionService.BroadcastCharacterUpdateAsync(cr.CharacterId);
    }

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
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            var pool = JsonSerializer.Deserialize<PoolDefinition>(cr.SorceryRiteDefinition.PoolDefinitionJson, options);
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
}
