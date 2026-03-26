using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for blood sorcery rite learning and activation.
/// </summary>
public class SorceryService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    IBeatLedgerService beatLedger,
    ISessionService sessionService,
    ILogger<SorceryService> logger) : ISorceryService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly ISessionService _sessionService = sessionService;
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
            .Include(c => c.Clan)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CovenantId == null || character.CovenantJoinStatus != null)
        {
            return [];
        }

        var learnedOrPendingIds = character.Rites
            .Where(r => r.Status == RiteLearnStatus.Approved || r.Status == RiteLearnStatus.Pending)
            .Select(r => r.SorceryRiteDefinitionId)
            .ToHashSet();

        var query = await _dbContext.SorceryRiteDefinitions
            .AsNoTracking()
            .Include(s => s.RequiredCovenant)
            .Include(s => s.RequiredClan)
            .Where(r => (r.RequiredCovenantId == null || r.RequiredCovenantId == character.CovenantId)
                && (r.RequiredClanId == null || r.RequiredClanId == character.ClanId)
                && !learnedOrPendingIds.Contains(r.Id)
                && GetSorceryDisciplineRating(character, r.SorceryType) >= r.Level
                && IsTraditionAllowedForCharacter(character, r.SorceryType))
            .OrderBy(r => r.SorceryType)
            .ThenBy(r => r.Level)
            .ThenBy(r => r.Name)
            .ToListAsync();

        return query.Select(r => new SorceryRiteSummaryDto(
            r.Id,
            r.Name,
            r.Level,
            r.SorceryType,
            r.XpCost,
            SummarizeRiteGate(r))).ToList();
    }

    /// <inheritdoc />
    public async Task<CharacterRite> RequestLearnRiteAsync(int characterId, int sorceryRiteDefinitionId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "request rite learning");

        Character character = await _dbContext.Characters
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
            .Include(c => c.Rites)
            .Include(c => c.Covenant)
            .Include(c => c.Clan)
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

        if (rite.RequiredCovenantId.HasValue && rite.RequiredCovenantId.Value != character.CovenantId)
        {
            throw new InvalidOperationException("Character's covenant does not match the rite's required covenant.");
        }

        if (rite.RequiredClanId.HasValue && rite.RequiredClanId.Value != character.ClanId)
        {
            throw new InvalidOperationException("Character's clan does not match the rite's required clan.");
        }

        if (!IsTraditionAllowedForCharacter(character, rite.SorceryType))
        {
            throw new InvalidOperationException("Character's covenant does not support this sorcery tradition.");
        }

        int disciplineRating = GetSorceryDisciplineRating(character, rite.SorceryType);
        if (disciplineRating < rite.Level)
        {
            throw new InvalidOperationException($"Character needs sufficient discipline dots ({rite.SorceryType} level {rite.Level}) to learn this rite.");
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

        _logger.LogInformation(
            "Deducting {XpCost} XP for rite '{RiteName}' on character {CharacterId}",
            rite.XpCost,
            rite.Name,
            character.Id);

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

    private static int GetSorceryDisciplineRating(Character character, SorceryType sorceryType) =>
        sorceryType switch
        {
            SorceryType.Cruac => character.GetDisciplineRating("Crúac"),
            SorceryType.Theban => character.GetDisciplineRating("Theban Sorcery"),
            SorceryType.Necromancy => character.GetDisciplineRating("Necromancy"),
            SorceryType.OrdoDraculRitual => character.GetDisciplineRating("Ordo Sorcery"),
            _ => 0,
        };

    private static bool IsTraditionAllowedForCharacter(Character character, SorceryType type) =>
        type switch
        {
            SorceryType.Cruac or SorceryType.Theban => character.Covenant?.SupportsBloodSorcery == true,
            SorceryType.OrdoDraculRitual => character.Covenant?.SupportsOrdoRituals == true,
            SorceryType.Necromancy => true,
            _ => false,
        };

    private static string SummarizeRiteGate(SorceryRiteDefinition r)
    {
        if (r.RequiredCovenant != null)
        {
            return r.RequiredCovenant.Name;
        }

        if (r.RequiredClan != null)
        {
            return $"{r.RequiredClan.Name} (clan)";
        }

        return "—";
    }
}
