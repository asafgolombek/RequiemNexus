using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Observability;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public class SocialManeuverLifecycleCoordinator(
    IConditionService conditionService,
    ISessionPublisher sessionPublisher,
    ILogger<SocialManeuverLifecycleCoordinator> logger) : ISocialManeuverLifecycleCoordinator
{
    private readonly IConditionService _conditionService = conditionService;
    private readonly ISessionPublisher _sessionPublisher = sessionPublisher;
    private readonly ILogger<SocialManeuverLifecycleCoordinator> _logger = logger;

    /// <inheritdoc />
    public async Task<SocialManeuver> LoadManeuverForMutationAsync(ApplicationDbContext db, int maneuverId)
    {
        SocialManeuver maneuver = await db.SocialManeuvers
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .Include(m => m.Interceptors)
            .ThenInclude(i => i.InterceptorCharacter)
            .FirstOrDefaultAsync(m => m.Id == maneuverId)
            ?? throw new InvalidOperationException($"Social maneuver {maneuverId} not found.");

        await ApplyHostileWeekFailureIfNeededAsync(db, maneuver, DateTimeOffset.UtcNow);
        return maneuver;
    }

    /// <inheritdoc />
    public async Task PublishManeuverUpdateAsync(ApplicationDbContext db, int maneuverId)
    {
        SocialManeuver? row = await db.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .FirstOrDefaultAsync(m => m.Id == maneuverId);

        if (row == null)
        {
            return;
        }

        var full = new SocialManeuverUpdateDto(
            row.CampaignId,
            row.Id,
            row.InitiatorCharacterId,
            row.InitiatorCharacter?.Name ?? "?",
            row.TargetChronicleNpcId,
            row.TargetNpc?.Name ?? "?",
            row.RemainingDoors,
            row.InitialDoors,
            row.CurrentImpression,
            row.Status,
            row.CumulativePenaltyDice,
            row.LastRollAt,
            row.GoalDescription);

        SocialManeuverUpdateDto redacted = full with { GoalDescription = string.Empty };

        await _sessionPublisher.Group(row.CampaignId).ReceiveSocialManeuverUpdate(redacted);

        string? stUserId = row.Campaign?.StoryTellerId;
        if (!string.IsNullOrEmpty(stUserId))
        {
            await _sessionPublisher.User(stUserId).ReceiveSocialManeuverUpdate(full);
        }
    }

    /// <inheritdoc />
    public async Task ApplySocialConditionIfAbsentAsync(
        ApplicationDbContext db,
        int characterId,
        ConditionType type,
        string? descriptionOverride,
        string actingUserId)
    {
        bool hasActive = await db.CharacterConditions.AnyAsync(
            c => c.CharacterId == characterId && !c.IsResolved && c.ConditionType == type);

        if (hasActive)
        {
            return;
        }

        await _conditionService.ApplyConditionAsync(characterId, type, null, descriptionOverride, actingUserId);
    }

    private async Task ApplyHostileWeekFailureIfNeededAsync(ApplicationDbContext db, SocialManeuver maneuver, DateTimeOffset nowUtc)
    {
        if (maneuver.Status != ManeuverStatus.Active)
        {
            return;
        }

        if (!SocialManeuveringEngine.ShouldFailFromHostileWeek(maneuver.HostileSince, maneuver.CurrentImpression, nowUtc))
        {
            return;
        }

        maneuver.Status = ManeuverStatus.Failed;
        string correlationId = AmbientCorrelation.ForNewOperation();
        _logger.LogInformation(
            "Maneuver {ManeuverId} failed: Hostile impression persisted for one week. {CorrelationId}",
            maneuver.Id,
            correlationId);

        await db.SaveChangesAsync();

        string stUserId = maneuver.Campaign?.StoryTellerId
            ?? await db.Campaigns.AsNoTracking()
                .Where(c => c.Id == maneuver.CampaignId)
                .Select(c => c.StoryTellerId)
                .FirstAsync();

        await ApplySocialConditionIfAbsentAsync(
            db,
            maneuver.InitiatorCharacterId,
            ConditionType.Shaken,
            "From Social maneuver failure: Hostile impression lasted a week.",
            stUserId);

        await PublishManeuverUpdateAsync(db, maneuver.Id);
    }
}
