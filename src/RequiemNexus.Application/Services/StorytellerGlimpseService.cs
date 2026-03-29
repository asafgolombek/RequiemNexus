using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Implements <see cref="IStorytellerGlimpseService"/>: ST-only dashboard operations for
/// reading character vitals and awarding Beats / XP to campaign characters.
/// </summary>
public class StorytellerGlimpseService(
    ApplicationDbContext dbContext,
    IBeatLedgerService beatLedger,
    ICharacterCreationRules creationRules,
    ILogger<StorytellerGlimpseService> logger,
    ISessionService sessionService) : IStorytellerGlimpseService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly ICharacterCreationRules _creationRules = creationRules;
    private readonly ILogger _logger = logger;

    /// <inheritdoc />
    public async Task<List<CharacterVitalsDto>> GetCampaignVitalsAsync(int campaignId, string storyTellerUserId)
    {
        await RequireStorytellerAsync(campaignId, storyTellerUserId);

        // Projected query: fetch only needed columns (no SELECT *)
        var characterData = await _dbContext.Characters
            .Where(c => c.CampaignId == campaignId)
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.ApplicationUserId,
                c.CurrentHealth,
                c.MaxHealth,
                c.HealthDamage,
                c.CurrentWillpower,
                c.MaxWillpower,
                c.CurrentVitae,
                c.MaxVitae,
                c.Humanity,
                c.HumanityStains,
                c.Touchstone,
                ResolveRating = c.Attributes
                    .Where(a => a.Name == nameof(AttributeId.Resolve))
                    .Select(a => (int?)a.Rating)
                    .FirstOrDefault() ?? 0,
                TouchstoneMeritDots = c.Merits
                    .Where(m => m.Merit != null && m.Merit.Name == "Touchstone")
                    .Sum(m => m.Rating),
                c.Beats,
                c.ExperiencePoints,
                c.CreatureType,
            })
            .ToListAsync();

        List<int> characterIds = characterData.Select(c => c.Id).ToList();

        // Count active conditions per character in a single query
        Dictionary<int, int> activeConditionCounts = await _dbContext.CharacterConditions
            .Where(c => characterIds.Contains(c.CharacterId) && !c.IsResolved)
            .GroupBy(c => c.CharacterId)
            .Select(g => new { CharacterId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CharacterId, x => x.Count);

        return characterData.Select(c => new CharacterVitalsDto
        {
            CharacterId = c.Id,
            Name = c.Name,
            PlayerUserId = c.ApplicationUserId,
            CurrentHealth = c.CurrentHealth,
            MaxHealth = c.MaxHealth,
            HealthDamage = c.HealthDamage ?? string.Empty,
            CurrentWillpower = c.CurrentWillpower,
            MaxWillpower = c.MaxWillpower,
            CurrentVitae = c.CurrentVitae,
            MaxVitae = c.MaxVitae,
            Humanity = c.Humanity,
            HumanityStains = c.HumanityStains,
            ResolveRating = c.ResolveRating,
            HasTouchstoneAnchor = !string.IsNullOrWhiteSpace(c.Touchstone) || c.TouchstoneMeritDots > 0,
            Beats = c.Beats,
            ExperiencePoints = c.ExperiencePoints,
            ActiveConditionCount = activeConditionCounts.GetValueOrDefault(c.Id, 0),
            CreatureType = c.CreatureType,
        }).ToList();
    }

    /// <inheritdoc />
    public async Task AwardBeatToCharacterAsync(
        int campaignId,
        int characterId,
        string reason,
        string storyTellerUserId)
    {
        await RequireStorytellerAsync(campaignId, storyTellerUserId);

        Character character = await RequireCharacterInCampaignAsync(characterId, campaignId);

        await AwardBeatInternalAsync(character, campaignId, reason, storyTellerUserId);
        await _dbContext.SaveChangesAsync();

        await sessionService.BroadcastCharacterUpdateAsync(characterId);

        _logger.LogInformation(
            "Storyteller {StoryTellerId} awarded Beat to character {CharacterId} in campaign {CampaignId}. Reason: {Reason}",
            storyTellerUserId,
            characterId,
            campaignId,
            reason);
    }

    /// <inheritdoc />
    public async Task AwardBeatToCampaignAsync(int campaignId, string reason, string storyTellerUserId)
    {
        await RequireStorytellerAsync(campaignId, storyTellerUserId);

        List<Character> characters = await _dbContext.Characters
            .Where(c => c.CampaignId == campaignId)
            .ToListAsync();

        // Guard against unbounded N+1 latency: each character triggers RecordBeatAsync,
        // RecordXpCreditAsync (if beat conversion), and BroadcastCharacterUpdateAsync.
        const int maxBatchSize = 50;
        if (characters.Count > maxBatchSize)
        {
            throw new InvalidOperationException(
                $"Awarding Beats to all characters is limited to campaigns with at most {maxBatchSize} characters. " +
                $"This campaign has {characters.Count}. Award Beats per character instead.");
        }

        foreach (Character character in characters)
        {
            await AwardBeatInternalAsync(character, campaignId, reason, storyTellerUserId);
        }

        await _dbContext.SaveChangesAsync();

        foreach (Character character in characters)
        {
            await sessionService.BroadcastCharacterUpdateAsync(character.Id);
        }

        _logger.LogInformation(
            "Storyteller {StoryTellerId} awarded Beat to all {Count} characters in campaign {CampaignId}. Reason: {Reason}",
            storyTellerUserId,
            characters.Count,
            campaignId,
            reason);
    }

    /// <inheritdoc />
    public async Task AwardXpToCharacterAsync(
        int campaignId,
        int characterId,
        int amount,
        string reason,
        string storyTellerUserId)
    {
        await RequireStorytellerAsync(campaignId, storyTellerUserId);

        Character character = await RequireCharacterInCampaignAsync(characterId, campaignId);

        character.ExperiencePoints += amount;
        character.TotalExperiencePoints += amount;

        await _beatLedger.RecordXpCreditAsync(
            characterId,
            campaignId,
            amount,
            XpSource.StorytellerAward,
            reason,
            storyTellerUserId);

        await _dbContext.SaveChangesAsync();

        await sessionService.BroadcastCharacterUpdateAsync(characterId);

        _logger.LogInformation(
            "Storyteller {StoryTellerId} awarded {Amount} XP to character {CharacterId} in campaign {CampaignId}. Reason: {Reason}",
            storyTellerUserId,
            amount,
            characterId,
            campaignId,
            reason);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Increments a character's Beat count, writes the Beat ledger entry, and handles the
    /// 5-Beat → 1-XP conversion. Does NOT call <c>SaveChangesAsync</c> — the caller must do that.
    /// </summary>
    private async Task AwardBeatInternalAsync(
        Character character,
        int campaignId,
        string reason,
        string storyTellerUserId)
    {
        character.Beats++;

        await _beatLedger.RecordBeatAsync(
            character.Id,
            campaignId,
            BeatSource.StorytellerAward,
            reason,
            storyTellerUserId);

        if (_creationRules.TryConvertBeats(character.Beats, out int newBeats, out int xpGained))
        {
            character.Beats = newBeats;
            character.ExperiencePoints += xpGained;
            character.TotalExperiencePoints += xpGained;

            await _beatLedger.RecordXpCreditAsync(
                character.Id,
                campaignId,
                xpGained,
                XpSource.BeatConversion,
                $"Converted 5 Beats to {xpGained} XP",
                null);
        }
    }

    private async Task RequireStorytellerAsync(int campaignId, string storyTellerUserId)
    {
        bool isSt = await _dbContext.Campaigns
            .AnyAsync(c => c.Id == campaignId && c.StoryTellerId == storyTellerUserId);

        if (!isSt)
        {
            throw new UnauthorizedAccessException("Only the Storyteller of this campaign may perform this action.");
        }
    }

    private async Task<Character> RequireCharacterInCampaignAsync(int characterId, int campaignId)
    {
        return await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId && c.CampaignId == campaignId)
            ?? throw new InvalidOperationException($"Character {characterId} not found in campaign {campaignId}.");
    }
}
