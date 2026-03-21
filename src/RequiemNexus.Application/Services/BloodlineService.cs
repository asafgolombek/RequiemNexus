using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for bloodline applications and Storyteller approval workflow.
/// </summary>
public class BloodlineService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ISessionService sessionService,
    ILogger<BloodlineService> logger) : IBloodlineService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ILogger<BloodlineService> _logger = logger;

    /// <inheritdoc />
    public async Task<List<BloodlineSummaryDto>> GetEligibleBloodlinesAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "view eligible bloodlines");

        Character character = await _dbContext.Characters
            .AsNoTracking()
            .Include(c => c.Bloodlines)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.Bloodlines.Any(b => b.Status == BloodlineStatus.Active || b.Status == BloodlineStatus.Pending))
        {
            return [];
        }

        if (!character.ClanId.HasValue || character.CampaignId == null)
        {
            return [];
        }

        List<BloodlineDefinition> candidates = await _dbContext.BloodlineDefinitions
            .AsNoTracking()
            .Include(b => b.AllowedParentClans)
            .Where(b => b.AllowedParentClans.Any(bc => bc.ClanId == character.ClanId.Value)
                && character.BloodPotency >= b.PrerequisiteBloodPotency)
            .ToListAsync();

        var result = new List<BloodlineSummaryDto>();
        foreach (BloodlineDefinition b in candidates)
        {
            IReadOnlyList<int> allowedClanIds = b.AllowedParentClans.Select(bc => bc.ClanId).ToList();
            Result<bool> validation = BloodlineEngine.ValidateJoinPrerequisites(
                character.ClanId,
                character.BloodPotency,
                allowedClanIds,
                b.PrerequisiteBloodPotency);

            if (validation.IsSuccess)
            {
                result.Add(new BloodlineSummaryDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                });
            }
        }

        return result.OrderBy(b => b.Name).ToList();
    }

    /// <inheritdoc />
    public async Task<CharacterBloodline> ApplyForBloodlineAsync(int characterId, int bloodlineDefinitionId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "apply for bloodline");

        Character character = await _dbContext.Characters
            .Include(c => c.Bloodlines)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.CampaignId == null)
        {
            throw new InvalidOperationException("Character must be in a campaign to apply for a bloodline.");
        }

        if (character.Bloodlines.Any(b => b.Status == BloodlineStatus.Active))
        {
            throw new InvalidOperationException("Character already has an active bloodline.");
        }

        if (character.Bloodlines.Any(b => b.Status == BloodlineStatus.Pending))
        {
            throw new InvalidOperationException("Character already has a pending bloodline application.");
        }

        BloodlineDefinition? bloodline = await _dbContext.BloodlineDefinitions
            .Include(b => b.AllowedParentClans)
            .FirstOrDefaultAsync(b => b.Id == bloodlineDefinitionId)
            ?? throw new InvalidOperationException($"Bloodline {bloodlineDefinitionId} not found.");

        IReadOnlyList<int> allowedClanIds = bloodline.AllowedParentClans.Select(bc => bc.ClanId).ToList();
        Result<bool> validation = BloodlineEngine.ValidateJoinPrerequisites(
            character.ClanId,
            character.BloodPotency,
            allowedClanIds,
            bloodline.PrerequisiteBloodPotency);

        if (!validation.IsSuccess)
        {
            throw new InvalidOperationException(validation.Error);
        }

        var cb = new CharacterBloodline
        {
            CharacterId = characterId,
            BloodlineDefinitionId = bloodlineDefinitionId,
            Status = BloodlineStatus.Pending,
            AppliedAt = DateTime.UtcNow,
        };
        _dbContext.CharacterBloodlines.Add(cb);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Bloodline application submitted: Character {CharacterId}, Bloodline {BloodlineId}",
            characterId,
            bloodlineDefinitionId);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
        return cb;
    }

    /// <inheritdoc />
    public async Task<List<BloodlineApplicationDto>> GetPendingBloodlineApplicationsAsync(int campaignId, string storyTellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "view pending bloodline applications");

        return await _dbContext.CharacterBloodlines
            .AsNoTracking()
            .Include(cb => cb.Character)
            .Include(cb => cb.BloodlineDefinition)
            .Where(cb => cb.Status == BloodlineStatus.Pending
                && cb.Character != null
                && cb.Character.CampaignId == campaignId)
            .OrderByDescending(cb => cb.AppliedAt)
            .Select(cb => new BloodlineApplicationDto(
                cb.Id,
                cb.CharacterId,
                cb.Character!.Name,
                cb.BloodlineDefinition!.Name,
                cb.AppliedAt))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task ApproveBloodlineAsync(int characterBloodlineId, string? note, string storyTellerUserId)
    {
        CharacterBloodline? cb = await _dbContext.CharacterBloodlines
            .Include(cb => cb.Character)
            .Include(cb => cb.BloodlineDefinition)
            .FirstOrDefaultAsync(cb => cb.Id == characterBloodlineId)
            ?? throw new InvalidOperationException($"Bloodline application {characterBloodlineId} not found.");

        if (cb.Character?.CampaignId == null)
        {
            throw new InvalidOperationException("Application is not associated with a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(cb.Character.CampaignId.Value, storyTellerUserId, "approve bloodline application");

        cb.Status = BloodlineStatus.Active;
        cb.ResolvedAt = DateTime.UtcNow;
        cb.StorytellerNote = note;

        List<CharacterBloodline> otherPending = await _dbContext.CharacterBloodlines
            .Where(b => b.CharacterId == cb.CharacterId
                && b.Id != characterBloodlineId
                && b.Status == BloodlineStatus.Pending)
            .ToListAsync();

        foreach (CharacterBloodline other in otherPending)
        {
            other.Status = BloodlineStatus.Rejected;
            other.ResolvedAt = DateTime.UtcNow;
            other.StorytellerNote = "Superseded by approval of another bloodline.";
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Bloodline application approved: CharacterBloodline {Id}, Character {CharacterId}",
            characterBloodlineId,
            cb.CharacterId);

        await _sessionService.BroadcastCharacterUpdateAsync(cb.CharacterId);
        await _sessionService.BroadcastBloodlineApprovedAsync(
            cb.CharacterId,
            cb.BloodlineDefinition?.Name ?? "Bloodline");
    }

    /// <inheritdoc />
    public async Task RejectBloodlineAsync(int characterBloodlineId, string? note, string storyTellerUserId)
    {
        CharacterBloodline? cb = await _dbContext.CharacterBloodlines
            .Include(cb => cb.Character)
            .FirstOrDefaultAsync(cb => cb.Id == characterBloodlineId)
            ?? throw new InvalidOperationException($"Bloodline application {characterBloodlineId} not found.");

        if (cb.Character?.CampaignId == null)
        {
            throw new InvalidOperationException("Application is not associated with a campaign.");
        }

        await _authHelper.RequireStorytellerAsync(cb.Character.CampaignId.Value, storyTellerUserId, "reject bloodline application");

        cb.Status = BloodlineStatus.Rejected;
        cb.ResolvedAt = DateTime.UtcNow;
        cb.StorytellerNote = note;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Bloodline application rejected: CharacterBloodline {Id}, Character {CharacterId}",
            characterBloodlineId,
            cb.CharacterId);

        await _sessionService.BroadcastCharacterUpdateAsync(cb.CharacterId);
    }

    /// <inheritdoc />
    public async Task RemoveBloodlineAsync(int characterBloodlineId, string userId)
    {
        CharacterBloodline? cb = await _dbContext.CharacterBloodlines
            .Include(cb => cb.Character)
            .Include(cb => cb.BloodlineDefinition)
            .FirstOrDefaultAsync(cb => cb.Id == characterBloodlineId)
            ?? throw new InvalidOperationException($"Bloodline {characterBloodlineId} not found.");

        await _authHelper.RequireCharacterOwnerAsync(cb.CharacterId, userId, "remove bloodline");

        if (cb.Status != BloodlineStatus.Active)
        {
            throw new InvalidOperationException("Only active bloodlines can be removed.");
        }

        int bloodlineDefinitionId = cb.BloodlineDefinitionId;
        int characterId = cb.CharacterId;

        // Remove devotions that require this bloodline
        List<CharacterDevotion> devotionsToRemove = await _dbContext.CharacterDevotions
            .Include(cd => cd.DevotionDefinition)
            .Where(cd => cd.CharacterId == characterId
                && cd.DevotionDefinition != null
                && cd.DevotionDefinition.RequiredBloodlineId == bloodlineDefinitionId)
            .ToListAsync();

        foreach (CharacterDevotion cd in devotionsToRemove)
        {
            _dbContext.CharacterDevotions.Remove(cd);
        }

        _dbContext.CharacterBloodlines.Remove(cb);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Bloodline removed: CharacterBloodline {Id}, Character {CharacterId}, removed {DevotionCount} devotions",
            characterBloodlineId,
            characterId,
            devotionsToRemove.Count);

        await _sessionService.BroadcastCharacterUpdateAsync(characterId);
    }
}
