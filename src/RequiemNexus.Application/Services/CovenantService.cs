using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for Covenant join/leave and Storyteller approval workflow.
/// </summary>
public class CovenantService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ISessionService sessionService,
    IReferenceDataCache referenceData,
    ILogger<CovenantService> logger) : ICovenantService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISessionService _sessionService = sessionService;
    private readonly IReferenceDataCache _referenceData = referenceData;
    private readonly ILogger<CovenantService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<CovenantSummaryDto>> GetEligibleCovenantsAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "view eligible covenants");

        Character character = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            return [];
        }

        if (character.CovenantJoinStatus == CovenantJoinStatus.Pending)
        {
            return [];
        }

        List<CovenantDefinition> candidates = _referenceData.CovenantDefinitions
            .Where(c => c.IsPlayable)
            .OrderBy(c => c.Name)
            .ToList();

        return candidates.Select(c => new CovenantSummaryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
        }).ToList();
    }

    /// <inheritdoc />
    public async Task ApplyForCovenantAsync(int characterId, int covenantDefinitionId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "apply for covenant");

        Character character = await _dbContext.Characters
            .Include(c => c.Covenant)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character must be in a campaign to apply for a covenant.");
        }

        CovenantDefinition? covenant = _referenceData.CovenantDefinitions
            .FirstOrDefault(c => c.Id == covenantDefinitionId)
            ?? throw new InvalidOperationException($"Covenant {covenantDefinitionId} not found.");

        if (!covenant.IsPlayable)
        {
            throw new InvalidOperationException("This covenant cannot be joined by player characters.");
        }

        if (character.CovenantJoinStatus == CovenantJoinStatus.Pending)
        {
            throw new InvalidOperationException("Character already has a pending covenant application.");
        }

        if (character.CovenantId.HasValue && character.CovenantJoinStatus == null)
        {
            throw new InvalidOperationException("Character is already in a covenant. Request to leave first.");
        }

        character.CovenantId = covenantDefinitionId;
        character.CovenantJoinStatus = CovenantJoinStatus.Pending;
        character.CovenantAppliedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Covenant application submitted: Character {CharacterId}, Covenant {CovenantId}",
            characterId,
            covenantDefinitionId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task<List<CovenantApplicationDto>> GetPendingCovenantApplicationsAsync(int campaignId, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "view pending covenant applications");

        return await _dbContext.Characters
            .AsNoTracking()
            .Include(c => c.Covenant)
            .Where(c => c.CampaignId == campaignId
                && c.CovenantJoinStatus == CovenantJoinStatus.Pending
                && c.CovenantId != null
                && c.Covenant != null)
            .OrderByDescending(c => c.CovenantAppliedAt)
            .Select(c => new CovenantApplicationDto(
                c.Id,
                c.Name,
                c.Covenant!.Name,
                c.CovenantAppliedAt ?? DateTime.MinValue))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ApproveCovenantAsync(int characterId, string? note, string storyTellerUserId)
    {
        Character? character = await _dbContext.Characters
            .Include(c => c.Covenant)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "approve covenant application");

        if (character.CovenantJoinStatus != CovenantJoinStatus.Pending)
        {
            throw new InvalidOperationException("Character does not have a pending covenant application.");
        }

        character.CovenantJoinStatus = null;
        character.CovenantAppliedAt = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Covenant application approved: Character {CharacterId}, Covenant {CovenantName}",
            characterId,
            character.Covenant?.Name ?? "Unknown");

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task RejectCovenantAsync(int characterId, string? note, string storyTellerUserId)
    {
        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "reject covenant application");

        if (character.CovenantJoinStatus != CovenantJoinStatus.Pending)
        {
            throw new InvalidOperationException("Character does not have a pending covenant application.");
        }

        character.CovenantId = null;
        character.CovenantJoinStatus = null;
        character.CovenantAppliedAt = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Covenant application rejected: Character {CharacterId}",
            characterId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task KickFromCovenantAsync(int characterId, string storyTellerUserId)
    {
        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "kick character from covenant");

        if (!character.CovenantId.HasValue || character.CovenantJoinStatus == CovenantJoinStatus.Pending)
        {
            throw new InvalidOperationException("Character is not in an approved covenant.");
        }

        character.CovenantId = null;
        character.CovenantJoinStatus = null;
        character.CovenantAppliedAt = null;
        character.CovenantLeaveRequestedAt = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Character kicked from covenant: Character {CharacterId} by ST",
            characterId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task RequestLeaveCovenantAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "request to leave covenant");

        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (!character.CovenantId.HasValue || character.CovenantJoinStatus == CovenantJoinStatus.Pending)
        {
            throw new InvalidOperationException("Character is not in an approved covenant.");
        }

        if (character.CovenantLeaveRequestedAt.HasValue)
        {
            throw new InvalidOperationException("Leave request is already pending.");
        }

        character.CovenantLeaveRequestedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Covenant leave requested: Character {CharacterId}",
            characterId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task ApproveLeaveRequestAsync(int characterId, string storyTellerUserId)
    {
        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "approve covenant leave request");

        if (!character.CovenantLeaveRequestedAt.HasValue)
        {
            throw new InvalidOperationException("Character has not requested to leave.");
        }

        character.CovenantId = null;
        character.CovenantLeaveRequestedAt = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Covenant leave approved: Character {CharacterId}",
            characterId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }

    /// <inheritdoc />
    public async Task RejectLeaveRequestAsync(int characterId, string storyTellerUserId)
    {
        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character is not in a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(character.CampaignId.Value, storyTellerUserId, "reject covenant leave request");

        character.CovenantLeaveRequestedAt = null;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Covenant leave rejected: Character {CharacterId}",
            characterId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }
}
