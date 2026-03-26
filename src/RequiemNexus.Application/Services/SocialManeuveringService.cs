using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Persists and mutates <see cref="SocialManeuver"/> entities with Masquerade checks and server-side dice.
/// Uses <see cref="IDbContextFactory{TContext}"/> so Blazor Server circuits can overlap loads without sharing one scoped <see cref="ApplicationDbContext"/>.
/// </summary>
public class SocialManeuveringService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    IAuthorizationHelper authHelper,
    ISessionPublisher sessionPublisher,
    IConditionService conditionService,
    ILogger<SocialManeuveringService> logger) : ISocialManeuveringService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISessionPublisher _sessionPublisher = sessionPublisher;
    private readonly IConditionService _conditionService = conditionService;
    private readonly ILogger<SocialManeuveringService> _logger = logger;

    /// <inheritdoc />
    public async Task<SocialManeuver> CreateAsync(
        int campaignId,
        int initiatorCharacterId,
        int targetChronicleNpcId,
        string goalDescription,
        bool goalWouldBeBreakingPoint,
        bool goalPreventsAspiration,
        bool actsAgainstVirtueOrMask,
        string storytellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storytellerUserId, "create a Social maneuver");

        if (string.IsNullOrWhiteSpace(goalDescription))
        {
            throw new InvalidOperationException("Goal description is required.");
        }

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Character initiator = await db.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == initiatorCharacterId)
            ?? throw new InvalidOperationException($"Character {initiatorCharacterId} not found.");

        if (initiator.CampaignId != campaignId)
        {
            throw new InvalidOperationException("Initiator must belong to the campaign.");
        }

        ChronicleNpc target = await db.ChronicleNpcs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == targetChronicleNpcId)
            ?? throw new InvalidOperationException($"Chronicle NPC {targetChronicleNpcId} not found.");

        if (target.CampaignId != campaignId)
        {
            throw new InvalidOperationException("Target NPC must belong to the campaign.");
        }

        bool burntExists = await db.SocialManeuvers.AnyAsync(m =>
            m.InitiatorCharacterId == initiatorCharacterId
            && m.TargetChronicleNpcId == targetChronicleNpcId
            && m.Status == ManeuverStatus.Burnt);

        if (burntExists)
        {
            throw new InvalidOperationException(
                "This initiator has burnt the relationship with that NPC; Social maneuvering cannot begin again.");
        }

        (int resolve, int composure) = SocialManeuveringAttributeParser.ReadResolveComposure(target.AttributesJson);
        int initialDoors = SocialManeuveringEngine.ComputeInitialDoorCount(
            resolve,
            composure,
            goalWouldBeBreakingPoint,
            goalPreventsAspiration,
            actsAgainstVirtueOrMask);

        SocialManeuver maneuver = new()
        {
            CampaignId = campaignId,
            InitiatorCharacterId = initiatorCharacterId,
            TargetChronicleNpcId = targetChronicleNpcId,
            GoalDescription = goalDescription.Trim(),
            InitialDoors = initialDoors,
            RemainingDoors = initialDoors,
            CurrentImpression = ImpressionLevel.Average,
            Status = ManeuverStatus.Active,
        };

        db.SocialManeuvers.Add(maneuver);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Social maneuver {ManeuverId} created in campaign {CampaignId} (initiator {CharacterId}, target NPC {NpcId}, doors {Doors}) by ST {UserId}",
            maneuver.Id,
            campaignId,
            initiatorCharacterId,
            targetChronicleNpcId,
            initialDoors,
            storytellerUserId);

        await PublishManeuverUpdateAsync(db, maneuver.Id);

        return maneuver;
    }

    /// <inheritdoc />
    public async Task SetImpressionAsync(int maneuverId, ImpressionLevel impression, string storytellerUserId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await LoadManeuverForMutationAsync(db, maneuverId);
        await _authHelper.RequireStorytellerAsync(maneuver.CampaignId, storytellerUserId, "set maneuver impression");

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (impression == ImpressionLevel.Hostile && maneuver.CurrentImpression != ImpressionLevel.Hostile)
        {
            maneuver.HostileSince = now;
        }
        else if (impression != ImpressionLevel.Hostile)
        {
            maneuver.HostileSince = null;
        }

        maneuver.CurrentImpression = impression;
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver {ManeuverId} impression set to {Impression} by ST {UserId}",
            maneuverId,
            impression,
            storytellerUserId);

        await PublishManeuverUpdateAsync(db, maneuver.Id);
    }

    /// <inheritdoc />
    public async Task SetRemainingDoorsNarrativeAsync(int maneuverId, int remainingDoors, string storytellerUserId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await LoadManeuverForMutationAsync(db, maneuverId);
        await _authHelper.RequireStorytellerAsync(maneuver.CampaignId, storytellerUserId, "adjust maneuver Doors");

        if (remainingDoors < 0 || remainingDoors > maneuver.InitialDoors)
        {
            throw new InvalidOperationException(
                $"Remaining Doors must be between 0 and {maneuver.InitialDoors}.");
        }

        maneuver.RemainingDoors = remainingDoors;

        if (maneuver.Status == ManeuverStatus.Active && maneuver.RemainingDoors <= 0)
        {
            maneuver.Status = ManeuverStatus.Succeeded;
        }

        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver {ManeuverId} remaining Doors set to {Remaining} by ST {UserId}",
            maneuverId,
            remainingDoors,
            storytellerUserId);

        await PublishManeuverUpdateAsync(db, maneuver.Id);
    }

    /// <inheritdoc />
    public async Task SetInvestigationSuccessesPerClueAsync(int campaignId, int successesPerClue, string storytellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storytellerUserId, "configure Social maneuver investigation threshold");

        int clamped = Math.Clamp(successesPerClue, 1, 50);

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        Campaign campaign = await db.Campaigns.FindAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        campaign.SocialManeuverInvestigationSuccessesPerClue = clamped;
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Campaign {CampaignId} Social maneuver investigation successes-per-clue set to {Threshold} by ST {UserId}",
            campaignId,
            clamped,
            storytellerUserId);
    }

    /// <inheritdoc />
    public async Task BankInvestigationSuccessesAsync(int maneuverId, int successes, string userId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await LoadManeuverForMutationAsync(db, maneuverId);
        await _authHelper.RequireCharacterAccessAsync(maneuver.InitiatorCharacterId, userId, "bank Investigation successes");

        if (maneuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("Investigation successes can only be banked on an active maneuver.");
        }

        if (successes < 1)
        {
            throw new InvalidOperationException("Success count must be at least 1.");
        }

        Campaign campaign = maneuver.Campaign
            ?? await db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == maneuver.CampaignId)
            ?? throw new InvalidOperationException($"Campaign {maneuver.CampaignId} not found.");

        (int newProgress, int cluesGranted) = SocialManeuveringEngine.AccrueInvestigationTowardClues(
            maneuver.InvestigationProgressTowardNextClue,
            successes,
            campaign.SocialManeuverInvestigationSuccessesPerClue);

        maneuver.InvestigationProgressTowardNextClue = newProgress;

        for (int i = 0; i < cluesGranted; i++)
        {
            db.ManeuverClues.Add(new ManeuverClue
            {
                SocialManeuverId = maneuver.Id,
                SourceDescription = "Investigation — successes reached the chronicle clue threshold (Nexus).",
                LeverageKind = ClueLeverageKind.Soft,
            });
        }

        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Banked {Successes} Investigation successes on maneuver {ManeuverId}: cluesGranted={Clues}, newProgress={Progress}, user {UserId}",
            successes,
            maneuverId,
            cluesGranted,
            newProgress,
            userId);

        await PublishManeuverUpdateAsync(db, maneuver.Id);
    }

    /// <inheritdoc />
    public async Task<ManeuverClue> AddManeuverClueAsync(
        int maneuverId,
        string sourceDescription,
        ClueLeverageKind leverageKind,
        string storytellerUserId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await LoadManeuverForMutationAsync(db, maneuverId);
        await _authHelper.RequireStorytellerAsync(maneuver.CampaignId, storytellerUserId, "add a maneuver clue");

        if (string.IsNullOrWhiteSpace(sourceDescription))
        {
            throw new InvalidOperationException("Clue source description is required.");
        }

        ManeuverClue clue = new()
        {
            SocialManeuverId = maneuver.Id,
            SourceDescription = sourceDescription.Trim(),
            LeverageKind = leverageKind,
        };

        db.ManeuverClues.Add(clue);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver clue {ClueId} added to maneuver {ManeuverId} by ST {UserId}",
            clue.Id,
            maneuverId,
            storytellerUserId);

        await PublishManeuverUpdateAsync(db, maneuver.Id);

        return clue;
    }

    /// <inheritdoc />
    public async Task SpendManeuverClueAsync(int clueId, string benefit, string userId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        ManeuverClue clue = await db.ManeuverClues
            .Include(c => c.SocialManeuver)
            .ThenInclude(m => m!.TargetNpc)
            .FirstOrDefaultAsync(c => c.Id == clueId)
            ?? throw new InvalidOperationException($"Maneuver clue {clueId} not found.");

        if (clue.SocialManeuver is null)
        {
            throw new InvalidOperationException($"Maneuver clue {clueId} has no maneuver.");
        }

        await _authHelper.RequireCharacterAccessAsync(clue.SocialManeuver.InitiatorCharacterId, userId, "spend a maneuver clue");

        if (clue.IsSpent)
        {
            throw new InvalidOperationException("This clue has already been spent.");
        }

        if (string.IsNullOrWhiteSpace(benefit))
        {
            throw new InvalidOperationException("Recorded benefit text is required when spending a clue.");
        }

        clue.IsSpent = true;
        clue.Benefit = benefit.Trim();
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver clue {ClueId} spent on maneuver {ManeuverId} by user {UserId}",
            clueId,
            clue.SocialManeuverId,
            userId);

        await PublishManeuverUpdateAsync(db, clue.SocialManeuverId);
    }

    private async Task PublishManeuverUpdateAsync(ApplicationDbContext db, int maneuverId)
    {
        SocialManeuver? row = await db.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .FirstOrDefaultAsync(m => m.Id == maneuverId);

        if (row == null)
        {
            return;
        }

        var dto = new SocialManeuverUpdateDto(
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

        await _sessionPublisher.Group(row.CampaignId).ReceiveSocialManeuverUpdate(dto);
    }

    private async Task<SocialManeuver> LoadManeuverForMutationAsync(ApplicationDbContext db, int maneuverId)
    {
        SocialManeuver maneuver = await db.SocialManeuvers
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .FirstOrDefaultAsync(m => m.Id == maneuverId)
            ?? throw new InvalidOperationException($"Social maneuver {maneuverId} not found.");

        await ApplyHostileWeekFailureIfNeededAsync(db, maneuver, DateTimeOffset.UtcNow);
        return maneuver;
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
        _logger.LogInformation(
            "Maneuver {ManeuverId} failed: Hostile impression persisted for one week.",
            maneuver.Id);

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

    private async Task ApplySocialConditionIfAbsentAsync(
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
}
