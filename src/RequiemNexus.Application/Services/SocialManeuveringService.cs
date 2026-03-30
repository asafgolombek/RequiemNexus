using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
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
    ISocialManeuverLifecycleCoordinator lifecycleCoordinator,
    ILogger<SocialManeuveringService> logger) : ISocialManeuveringService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISocialManeuverLifecycleCoordinator _lifecycle = lifecycleCoordinator;
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

        await _lifecycle.PublishManeuverUpdateAsync(db, maneuver.Id);

        return maneuver;
    }

    /// <inheritdoc />
    public async Task SetImpressionAsync(int maneuverId, ImpressionLevel impression, string storytellerUserId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await _lifecycle.LoadManeuverForMutationAsync(db, maneuverId);
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

        await _lifecycle.PublishManeuverUpdateAsync(db, maneuver.Id);
    }

    /// <inheritdoc />
    public async Task SetRemainingDoorsNarrativeAsync(int maneuverId, int remainingDoors, string storytellerUserId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await _lifecycle.LoadManeuverForMutationAsync(db, maneuverId);
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

        await _lifecycle.PublishManeuverUpdateAsync(db, maneuver.Id);
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

        SocialManeuver maneuver = await _lifecycle.LoadManeuverForMutationAsync(db, maneuverId);
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

        await _lifecycle.PublishManeuverUpdateAsync(db, maneuver.Id);
    }

    /// <inheritdoc />
    public async Task<ManeuverClue> AddManeuverClueAsync(
        int maneuverId,
        string sourceDescription,
        ClueLeverageKind leverageKind,
        string storytellerUserId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await _lifecycle.LoadManeuverForMutationAsync(db, maneuverId);
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

        await _lifecycle.PublishManeuverUpdateAsync(db, maneuver.Id);

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

        await _lifecycle.PublishManeuverUpdateAsync(db, clue.SocialManeuverId);
    }

    /// <inheritdoc />
    public async Task<ManeuverInterceptor> AddInterceptorAsync(
        int socialManeuverId,
        int interceptorCharacterId,
        string storytellerUserId)
    {
        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        SocialManeuver maneuver = await _lifecycle.LoadManeuverForMutationAsync(db, socialManeuverId);
        await _authHelper.RequireStorytellerAsync(maneuver.CampaignId, storytellerUserId, "add a Social maneuver interceptor");

        if (maneuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("Interceptors can only be added to an active maneuver.");
        }

        if (interceptorCharacterId == maneuver.InitiatorCharacterId)
        {
            throw new InvalidOperationException("The initiator cannot be an interceptor on their own maneuver.");
        }

        Character interceptor = await db.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == interceptorCharacterId)
            ?? throw new InvalidOperationException($"Character {interceptorCharacterId} was not found.");

        if (interceptor.CampaignId != maneuver.CampaignId)
        {
            throw new InvalidOperationException("The interceptor must belong to the same campaign as the maneuver.");
        }

        bool exists = await db.ManeuverInterceptors.AnyAsync(i =>
            i.SocialManeuverId == socialManeuverId && i.InterceptorCharacterId == interceptorCharacterId);

        if (exists)
        {
            throw new InvalidOperationException("This character is already registered as an interceptor on this maneuver.");
        }

        var row = new ManeuverInterceptor
        {
            SocialManeuverId = socialManeuverId,
            InterceptorCharacterId = interceptorCharacterId,
            IsActive = true,
            Successes = 0,
        };

        db.ManeuverInterceptors.Add(row);
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Interceptor {InterceptorId} added to maneuver {ManeuverId} by ST {UserId}",
            row.Id,
            socialManeuverId,
            storytellerUserId);

        await _lifecycle.PublishManeuverUpdateAsync(db, socialManeuverId);
        return row;
    }

    /// <inheritdoc />
    public async Task RecordInterceptorRollAsync(int interceptorId, int successes, string storytellerUserId)
    {
        if (successes < 0 || successes > 50)
        {
            throw new InvalidOperationException("Interceptor successes must be between 0 and 50.");
        }

        await using ApplicationDbContext db = await _dbContextFactory.CreateDbContextAsync();

        ManeuverInterceptor row = await db.ManeuverInterceptors
            .Include(i => i.SocialManeuver)
            .FirstOrDefaultAsync(i => i.Id == interceptorId)
            ?? throw new InvalidOperationException($"Maneuver interceptor {interceptorId} was not found.");

        if (row.SocialManeuver is null)
        {
            throw new InvalidOperationException($"Maneuver interceptor {interceptorId} has no maneuver.");
        }

        await _authHelper.RequireStorytellerAsync(row.SocialManeuver.CampaignId, storytellerUserId, "record interceptor opposition");

        if (!row.IsActive || row.SocialManeuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("Interceptor rolls can only be recorded for active interceptors on active maneuvers.");
        }

        Character? interceptor = await SocialManeuverDicePoolAuthority.LoadInitiatorForDiceCapAsync(
            db,
            row.InterceptorCharacterId);

        if (interceptor == null)
        {
            throw new InvalidOperationException($"Character {row.InterceptorCharacterId} was not found.");
        }

        int maxPool = SocialManeuverDicePoolAuthority.GetManipulationPersuasionPool(interceptor);
        if (successes > maxPool)
        {
            throw new InvalidOperationException(
                $"Interceptor successes ({successes}) exceed Manipulation + Persuasion ({maxPool}) on the character sheet.");
        }

        row.Successes = successes;
        await db.SaveChangesAsync();

        _logger.LogInformation(
            "Interceptor roll recorded: interceptor row {InterceptorRowId} successes {Successes} by ST {UserId}",
            interceptorId,
            successes,
            storytellerUserId);

        await _lifecycle.PublishManeuverUpdateAsync(db, row.SocialManeuverId);
    }
}
