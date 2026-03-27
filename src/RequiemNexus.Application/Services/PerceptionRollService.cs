using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Storyteller-only hidden perception rolls (no persistence, no SignalR broadcast).
/// </summary>
public class PerceptionRollService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    IDiceService diceService,
    ILogger<PerceptionRollService> logger) : IPerceptionRollService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IDiceService _diceService = diceService;
    private readonly ILogger<PerceptionRollService> _logger = logger;

    /// <inheritdoc />
    public async Task<PerceptionRollResultDto> RollPerceptionAsync(
        int characterId,
        bool useAwareness,
        int penaltyDice,
        string storyTellerUserId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Character character = await db.Characters
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (!character.CampaignId.HasValue)
        {
            throw new InvalidOperationException("Character is not attached to a campaign.");
        }

        int campaignId = character.CampaignId.Value;
        await _authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "roll hidden perception");

        int wits = character.GetAttributeRating(AttributeId.Wits);
        int secondary = useAwareness
            ? character.GetSkillRating("Awareness")
            : character.GetAttributeRating(AttributeId.Composure);

        int pool = wits + secondary;
        int dice = Math.Max(0, pool - Math.Max(0, penaltyDice));
        string poolDescription = useAwareness ? "Wits + Awareness" : "Wits + Composure";

        Domain.Models.RollResult result = _diceService.Roll(dice, tenAgain: true);

        _logger.LogInformation(
            "Hidden perception roll for character {CharacterId} in campaign {CampaignId}: pool={Pool} ({Description}), successes={Successes}",
            characterId,
            campaignId,
            dice,
            poolDescription,
            result.Successes);

        return new PerceptionRollResultDto(
            result.Successes,
            result.DiceRolled,
            poolDescription,
            result.IsExceptionalSuccess,
            result.IsDramaticFailure);
    }
}
