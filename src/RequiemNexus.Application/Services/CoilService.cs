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
/// Application service for Ordo Dracul Coil and Scale management.
/// Enforces Ordo membership, prerequisite chain, XP costs (3/4 per dot, Crucible discount), and ST approval.
/// </summary>
public class CoilService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    IBeatLedgerService beatLedger,
    ISessionService sessionService,
    ILogger<CoilService> logger) : ICoilService
{
    private const string _ordoDraculName = "The Ordo Dracul";

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ILogger<CoilService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<ScaleSummaryDto>> GetScalesAsync()
    {
        return await _dbContext.ScaleDefinitions
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new ScaleSummaryDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                MysteryName = s.MysteryName,
                MaxLevel = s.MaxLevel,
            })
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<CoilSummaryDto>> GetEligibleCoilsAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "view eligible coils");

        var character = await _dbContext.Characters
            .AsNoTracking()
            .Include(c => c.Covenant)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Coils).ThenInclude(cc => cc.CoilDefinition)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (!IsOrdoDraculMember(character))
        {
            return [];
        }

        var learnedOrPendingCoilIds = character.Coils
            .Where(cc => cc.Status == CoilLearnStatus.Approved || cc.Status == CoilLearnStatus.Pending)
            .Select(cc => cc.CoilDefinitionId)
            .ToHashSet();

        var approvedCoilIds = character.Coils
            .Where(cc => cc.Status == CoilLearnStatus.Approved)
            .Select(cc => cc.CoilDefinitionId)
            .ToHashSet();

        int ordoStatusDots = GetOrdoStatusDots(character);

        var allCoils = await _dbContext.CoilDefinitions
            .AsNoTracking()
            .Include(c => c.Scale)
            .OrderBy(c => c.ScaleId)
            .ThenBy(c => c.Level)
            .ToListAsync();

        var result = new List<CoilSummaryDto>();
        foreach (var coil in allCoils)
        {
            if (learnedOrPendingCoilIds.Contains(coil.Id))
            {
                continue;
            }

            // Prerequisite chain: must hold the previous tier
            if (coil.PrerequisiteCoilId.HasValue && !approvedCoilIds.Contains(coil.PrerequisiteCoilId.Value))
            {
                continue;
            }

            bool isChosenMystery = character.ChosenMysteryScaleId.HasValue && coil.ScaleId == character.ChosenMysteryScaleId.Value;

            // Ordo Status cap for non-chosen Coils
            if (!isChosenMystery && character.ChosenMysteryScaleId.HasValue)
            {
                int existingNonChosenDots = character.Coils
                    .Count(cc => cc.Status == CoilLearnStatus.Approved && cc.CoilDefinition?.ScaleId != character.ChosenMysteryScaleId);

                if (existingNonChosenDots >= ordoStatusDots)
                {
                    continue;
                }
            }

            int xpCost = CalculateXpCost(isChosenMystery, character.HasCrucibleRitualAccess);

            result.Add(new CoilSummaryDto(
                coil.Id,
                coil.Name,
                coil.Description,
                coil.Level,
                coil.ScaleId,
                coil.Scale?.Name ?? string.Empty,
                coil.PrerequisiteCoilId,
                xpCost,
                coil.RollDescription,
                isChosenMystery));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<CharacterCoil> RequestLearnCoilAsync(int characterId, int coilDefinitionId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "request coil purchase");

        var character = await _dbContext.Characters
            .Include(c => c.Covenant)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Coils).ThenInclude(cc => cc.CoilDefinition)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character must be in a campaign to purchase Coils.");
        }

        if (!IsOrdoDraculMember(character))
        {
            throw new InvalidOperationException("Only Ordo Dracul members can purchase Coils.");
        }

        var coil = await _dbContext.CoilDefinitions
            .Include(c => c.Scale)
            .FirstOrDefaultAsync(c => c.Id == coilDefinitionId)
            ?? throw new InvalidOperationException($"Coil {coilDefinitionId} not found.");

        // Prerequisite chain check
        if (coil.PrerequisiteCoilId.HasValue)
        {
            bool hasPrerequisite = character.Coils.Any(cc =>
                cc.CoilDefinitionId == coil.PrerequisiteCoilId.Value && cc.Status == CoilLearnStatus.Approved);
            if (!hasPrerequisite)
            {
                throw new InvalidOperationException($"Character must hold the previous tier before purchasing {coil.Name}.");
            }
        }

        bool isChosenMystery = character.ChosenMysteryScaleId.HasValue && coil.ScaleId == character.ChosenMysteryScaleId.Value;

        // Ordo Status cap for non-chosen Coils
        if (!isChosenMystery && character.ChosenMysteryScaleId.HasValue)
        {
            int ordoStatusDots = GetOrdoStatusDots(character);
            int existingNonChosenDots = character.Coils
                .Count(cc => cc.Status == CoilLearnStatus.Approved && cc.CoilDefinition?.ScaleId != character.ChosenMysteryScaleId);

            if (existingNonChosenDots >= ordoStatusDots)
            {
                throw new InvalidOperationException(
                    $"Non-chosen Coil dots cannot exceed Ordo Dracul Status ({ordoStatusDots} dots). Increase your Status first.");
            }
        }

        if (character.Coils.Any(cc => cc.CoilDefinitionId == coilDefinitionId && cc.Status == CoilLearnStatus.Pending))
        {
            throw new InvalidOperationException("A purchase request for this Coil is already pending.");
        }

        if (character.Coils.Any(cc => cc.CoilDefinitionId == coilDefinitionId && cc.Status == CoilLearnStatus.Approved))
        {
            throw new InvalidOperationException("Character has already learned this Coil.");
        }

        int xpCost = CalculateXpCost(isChosenMystery, character.HasCrucibleRitualAccess);

        if (character.ExperiencePoints < xpCost)
        {
            throw new InvalidOperationException($"Insufficient XP. Requires {xpCost}, character has {character.ExperiencePoints}.");
        }

        var cc2 = new CharacterCoil
        {
            CharacterId = characterId,
            CoilDefinitionId = coilDefinitionId,
            Status = CoilLearnStatus.Pending,
            AppliedAt = DateTime.UtcNow,
        };

        _dbContext.CharacterCoils.Add(cc2);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Coil purchase requested: Character {CharacterId}, Coil {CoilId} ({CoilName})",
            characterId,
            coilDefinitionId,
            coil.Name);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
        return cc2;
    }

    /// <inheritdoc />
    public async Task<List<CoilApplicationDto>> GetPendingCoilApplicationsAsync(int campaignId, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "view pending coil applications");

        return await _dbContext.CharacterCoils
            .AsNoTracking()
            .Include(cc => cc.Character)
            .Include(cc => cc.CoilDefinition).ThenInclude(c => c!.Scale)
            .Where(cc => cc.Status == CoilLearnStatus.Pending
                && cc.Character != null
                && cc.Character.CampaignId == campaignId)
            .OrderByDescending(cc => cc.AppliedAt)
            .Select(cc => new CoilApplicationDto(
                cc.Id,
                cc.CharacterId,
                cc.Character!.Name,
                cc.CoilDefinition!.Name,
                cc.CoilDefinition.Scale!.Name,
                cc.CoilDefinition.Level,
                (cc.Character.ChosenMysteryScaleId != null && cc.CoilDefinition.ScaleId == cc.Character.ChosenMysteryScaleId) ? (cc.Character.HasCrucibleRitualAccess ? 2 : 3) : (cc.Character.HasCrucibleRitualAccess ? 3 : 4),
                cc.AppliedAt))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ChosenMysteryApplicationDto>> GetPendingChosenMysteryApplicationsAsync(
        int campaignId,
        string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "view pending chosen mystery applications");

        return await _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.CampaignId == campaignId && c.PendingChosenMysteryScaleId != null)
            .OrderBy(c => c.Name)
            .Select(c => new ChosenMysteryApplicationDto(
                c.Id,
                c.Name,
                c.PendingChosenMysteryScaleId!.Value,
                c.PendingChosenMysteryScale!.Name))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ApproveCoilLearnAsync(int characterCoilId, string? note, string storyTellerUserId)
    {
        var cc = await _dbContext.CharacterCoils
            .Include(cc => cc.Character).ThenInclude(c => c!.Merits).ThenInclude(m => m.Merit)
            .Include(cc => cc.CoilDefinition).ThenInclude(c => c!.Scale)
            .FirstOrDefaultAsync(cc => cc.Id == characterCoilId)
            ?? throw new InvalidOperationException($"Coil application {characterCoilId} not found.");

        if (cc.Character?.CampaignId == null)
        {
            throw new InvalidOperationException("Application is not associated with a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(cc.Character.CampaignId.Value, storyTellerUserId, "approve coil purchase");

        if (cc.Status != CoilLearnStatus.Pending)
        {
            throw new InvalidOperationException("Only pending applications can be approved.");
        }

        var coil = cc.CoilDefinition!;
        var character = cc.Character!;

        bool isChosenMystery = character.ChosenMysteryScaleId.HasValue && coil.ScaleId == character.ChosenMysteryScaleId.Value;
        int xpCost = CalculateXpCost(isChosenMystery, character.HasCrucibleRitualAccess);

        _logger.LogInformation(
            "Deducting {XpCost} XP for Coil '{CoilName}' on character {CharacterId}",
            xpCost,
            coil.Name,
            character.Id);

        // Atomic XP deduction: prevents race condition from concurrent approvals
        int rowsAffected = await _dbContext.Characters
            .Where(c => c.Id == character.Id && c.ExperiencePoints >= xpCost)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.ExperiencePoints, c => c.ExperiencePoints - xpCost)
                .SetProperty(c => c.TotalExperiencePoints, c => c.TotalExperiencePoints - xpCost));

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException(
                $"Character has insufficient XP for Coil cost {xpCost}, or a concurrent update occurred.");
        }

        await _beatLedger.RecordXpSpendAsync(
            character.Id,
            character.CampaignId,
            xpCost,
            XpExpense.Coil,
            $"Learned Coil: {coil.Scale?.Name} — {coil.Name} (Tier {coil.Level})",
            storyTellerUserId);

        cc.Status = CoilLearnStatus.Approved;
        cc.ResolvedAt = DateTime.UtcNow;
        cc.StorytellerNote = note;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Coil purchase approved: CharacterCoil {Id}, Character {CharacterId}, Coil {CoilName}",
            characterCoilId,
            cc.CharacterId,
            coil.Name);

        await _sessionService.BroadcastCharacterUpdateAsync(cc.CharacterId);
    }

    /// <inheritdoc />
    public async Task RejectCoilLearnAsync(int characterCoilId, string? note, string storyTellerUserId)
    {
        var cc = await _dbContext.CharacterCoils
            .Include(cc => cc.Character)
            .FirstOrDefaultAsync(cc => cc.Id == characterCoilId)
            ?? throw new InvalidOperationException($"Coil application {characterCoilId} not found.");

        if (cc.Character?.CampaignId == null)
        {
            throw new InvalidOperationException("Application is not associated with a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(cc.Character.CampaignId.Value, storyTellerUserId, "reject coil purchase");

        if (cc.Status != CoilLearnStatus.Pending)
        {
            throw new InvalidOperationException("Only pending applications can be rejected.");
        }

        cc.Status = CoilLearnStatus.Rejected;
        cc.ResolvedAt = DateTime.UtcNow;
        cc.StorytellerNote = note;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Coil purchase rejected: CharacterCoil {Id}, Character {CharacterId}",
            characterCoilId,
            cc.CharacterId);

        await _sessionService.BroadcastCharacterUpdateAsync(cc.CharacterId);
    }

    /// <inheritdoc />
    public async Task RequestChosenMysteryAsync(int characterId, int scaleId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "request chosen mystery");

        var character = await _dbContext.Characters
            .Include(c => c.Covenant)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (!IsOrdoDraculMember(character))
        {
            throw new InvalidOperationException("Only Ordo Dracul members can select a Chosen Mystery.");
        }

        if (character.ChosenMysteryScaleId.HasValue)
        {
            throw new InvalidOperationException("Character already has an approved Chosen Mystery.");
        }

        if (character.PendingChosenMysteryScaleId.HasValue)
        {
            throw new InvalidOperationException("A Chosen Mystery selection is already pending approval.");
        }

        var scale = await _dbContext.ScaleDefinitions.FindAsync(scaleId)
            ?? throw new InvalidOperationException($"Scale {scaleId} not found.");

        character.PendingChosenMysteryScaleId = scaleId;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Chosen Mystery requested: Character {CharacterId}, Scale {ScaleName}",
            characterId,
            scale.Name);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task ApproveChosenMysteryAsync(int characterId, string storyTellerUserId)
    {
        var character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "approve chosen mystery");

        if (!character.PendingChosenMysteryScaleId.HasValue)
        {
            throw new InvalidOperationException("Character has no pending Chosen Mystery selection.");
        }

        character.ChosenMysteryScaleId = character.PendingChosenMysteryScaleId;
        character.PendingChosenMysteryScaleId = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Chosen Mystery approved: Character {CharacterId}, ScaleId {ScaleId}",
            characterId,
            character.ChosenMysteryScaleId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task RejectChosenMysteryAsync(int characterId, string storyTellerUserId)
    {
        var character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "reject chosen mystery");

        if (!character.PendingChosenMysteryScaleId.HasValue)
        {
            throw new InvalidOperationException("Character has no pending Chosen Mystery selection.");
        }

        character.PendingChosenMysteryScaleId = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Chosen Mystery rejected: Character {CharacterId}",
            characterId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task GrantCrucibleRitualAccessAsync(int characterId, string storyTellerUserId)
    {
        var character = await _dbContext.Characters
            .Include(c => c.Covenant)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "grant crucible ritual access");

        if (!IsOrdoDraculMember(character))
        {
            throw new InvalidOperationException("Only Ordo Dracul members can have Crucible Ritual access.");
        }

        character.HasCrucibleRitualAccess = true;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Crucible Ritual access granted: Character {CharacterId}", characterId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task RevokeCrucibleRitualAccessAsync(int characterId, string storyTellerUserId)
    {
        var character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "revoke crucible ritual access");

        character.HasCrucibleRitualAccess = false;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Crucible Ritual access revoked: Character {CharacterId}", characterId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    private static bool IsOrdoDraculMember(Character character)
    {
        return character.Covenant?.Name == _ordoDraculName
            && character.CovenantJoinStatus != Data.Models.Enums.CovenantJoinStatus.Pending;
    }

    private static int CalculateXpCost(bool isChosenMystery, bool hasCrucibleAccess)
    {
        return (isChosenMystery, hasCrucibleAccess) switch
        {
            (true, true) => 2,
            (true, false) => 3,
            (false, true) => 3,
            (false, false) => 4,
        };
    }

    private static int GetOrdoStatusDots(Character character)
    {
        // Ordo Dracul Status is a covenant-gated Merit. Check character merits for any
        // Status merit (naming convention: "Status (Ordo Dracul)" or similar).
        return character.Merits
            .Where(m => m.Merit != null
                && m.Merit.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                && m.Merit.Name.Contains("Ordo", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Rating);
    }
}
