using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Persists and mutates <see cref="SocialManeuver"/> entities with Masquerade checks and server-side dice.
/// </summary>
public class SocialManeuveringService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    IDiceService diceService,
    ISessionPublisher sessionPublisher,
    IConditionService conditionService,
    ILogger<SocialManeuveringService> logger) : ISocialManeuveringService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly IDiceService _diceService = diceService;
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

        Character initiator = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == initiatorCharacterId)
            ?? throw new InvalidOperationException($"Character {initiatorCharacterId} not found.");

        if (initiator.CampaignId != campaignId)
        {
            throw new InvalidOperationException("Initiator must belong to the campaign.");
        }

        ChronicleNpc target = await _dbContext.ChronicleNpcs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == targetChronicleNpcId)
            ?? throw new InvalidOperationException($"Chronicle NPC {targetChronicleNpcId} not found.");

        if (target.CampaignId != campaignId)
        {
            throw new InvalidOperationException("Target NPC must belong to the campaign.");
        }

        bool burntExists = await _dbContext.SocialManeuvers.AnyAsync(m =>
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

        _dbContext.SocialManeuvers.Add(maneuver);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Social maneuver {ManeuverId} created in campaign {CampaignId} (initiator {CharacterId}, target NPC {NpcId}, doors {Doors}) by ST {UserId}",
            maneuver.Id,
            campaignId,
            initiatorCharacterId,
            targetChronicleNpcId,
            initialDoors,
            storytellerUserId);

        await PublishManeuverUpdateAsync(maneuver.Id);

        return maneuver;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SocialManeuver>> ListForCampaignAsync(int campaignId, string storytellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storytellerUserId, "list Social maneuvers");

        return await _dbContext.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .Include(m => m.Clues)
            .Where(m => m.CampaignId == campaignId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SocialManeuver>> ListForInitiatorAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "view Social maneuvers");

        return await _dbContext.SocialManeuvers
            .AsNoTracking()
            .Include(m => m.InitiatorCharacter)
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .Include(m => m.Clues)
            .Where(m => m.InitiatorCharacterId == characterId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(SocialManeuver Updated, RollResult Roll, int DoorsOpened)> RollOpenDoorAsync(
        int maneuverId,
        int dicePool,
        string userId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
        await RequireInitiatorOrStorytellerAsync(maneuver, userId, "roll to open a Door");

        if (maneuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("This maneuver is no longer active.");
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        Result<bool> timing = SocialManeuveringEngine.ValidateOpenDoorRollTiming(
            maneuver.LastRollAt,
            maneuver.CurrentImpression,
            now);

        if (!timing.IsSuccess)
        {
            throw new InvalidOperationException(timing.Error);
        }

        int effectivePool = dicePool - maneuver.CumulativePenaltyDice;
        RollResult roll = _diceService.Roll(effectivePool);

        int doorsOpened = SocialManeuveringEngine.GetDoorsOpenedByOpenDoorRoll(
            roll.Successes,
            roll.IsExceptionalSuccess,
            roll.IsDramaticFailure);

        if (doorsOpened == 0 && !roll.IsDramaticFailure)
        {
            maneuver.CumulativePenaltyDice++;
        }

        maneuver.LastRollAt = now;
        maneuver.RemainingDoors = Math.Max(0, maneuver.RemainingDoors - doorsOpened);

        if (maneuver.RemainingDoors <= 0)
        {
            maneuver.Status = ManeuverStatus.Succeeded;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Open-Door roll on maneuver {ManeuverId}: successes={Successes}, doorsOpened={DoorsOpened}, remaining={Remaining}, user {UserId}",
            maneuverId,
            roll.Successes,
            doorsOpened,
            maneuver.RemainingDoors,
            userId);

        await ApplyOpenDoorOutcomeConditionsAsync(maneuver, roll, doorsOpened, userId);

        await PublishManeuverUpdateAsync(maneuver.Id);

        return (maneuver, roll, doorsOpened);
    }

    /// <inheritdoc />
    public async Task<(SocialManeuver Updated, RollResult Roll, bool ForcedSuccess)> RollForceDoorsAsync(
        int maneuverId,
        int dicePool,
        bool applyHardLeverage,
        int breakingPointSeverity,
        string userId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
        await RequireInitiatorOrStorytellerAsync(maneuver, userId, "force Doors");

        if (maneuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("This maneuver is no longer active.");
        }

        int closedDoors = maneuver.RemainingDoors;

        if (applyHardLeverage)
        {
            Character initiator = await _dbContext.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == maneuver.InitiatorCharacterId)
                ?? throw new InvalidOperationException($"Character {maneuver.InitiatorCharacterId} not found.");

            Result<int> removed = SocialManeuveringEngine.ComputeHardLeverageDoorsRemoved(
                breakingPointSeverity,
                initiator.Humanity);

            if (!removed.IsSuccess)
            {
                throw new InvalidOperationException(removed.Error);
            }

            closedDoors = Math.Max(0, closedDoors - removed.Value!);
        }

        int penalty = SocialManeuveringEngine.ComputeForceRollPoolPenalty(closedDoors);
        int effectivePool = dicePool - penalty;
        RollResult roll = _diceService.Roll(effectivePool);

        bool forcedSuccess = roll.Successes >= 1 && !roll.IsDramaticFailure;
        maneuver.LastRollAt = DateTimeOffset.UtcNow;

        if (forcedSuccess)
        {
            maneuver.RemainingDoors = 0;
            maneuver.Status = ManeuverStatus.Succeeded;
        }
        else
        {
            maneuver.Status = ManeuverStatus.Burnt;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Force-Doors roll on maneuver {ManeuverId}: successes={Successes}, success={ForcedSuccess}, status={Status}, user {UserId}",
            maneuverId,
            roll.Successes,
            forcedSuccess,
            maneuver.Status,
            userId);

        if (!forcedSuccess)
        {
            await ApplySocialConditionIfAbsentAsync(
                maneuver.InitiatorCharacterId,
                ConditionType.Shaken,
                "From failing to force Doors in Social maneuvering — the relationship is burnt.",
                userId);
        }
        else
        {
            string npcName = maneuver.TargetNpc?.Name ?? "the target";
            await ApplySocialConditionIfAbsentAsync(
                maneuver.InitiatorCharacterId,
                ConditionType.Swooned,
                $"From Social maneuver success (forced Doors) toward {npcName}.",
                userId);
        }

        await PublishManeuverUpdateAsync(maneuver.Id);

        return (maneuver, roll, forcedSuccess);
    }

    /// <inheritdoc />
    public async Task SetImpressionAsync(int maneuverId, ImpressionLevel impression, string storytellerUserId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
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
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver {ManeuverId} impression set to {Impression} by ST {UserId}",
            maneuverId,
            impression,
            storytellerUserId);

        await PublishManeuverUpdateAsync(maneuver.Id);
    }

    /// <inheritdoc />
    public async Task SetRemainingDoorsNarrativeAsync(int maneuverId, int remainingDoors, string storytellerUserId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
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

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver {ManeuverId} remaining Doors set to {Remaining} by ST {UserId}",
            maneuverId,
            remainingDoors,
            storytellerUserId);

        await PublishManeuverUpdateAsync(maneuver.Id);
    }

    /// <inheritdoc />
    public async Task SetInvestigationSuccessesPerClueAsync(int campaignId, int successesPerClue, string storytellerUserId)
    {
        await _authHelper.RequireStorytellerAsync(campaignId, storytellerUserId, "configure Social maneuver investigation threshold");

        int clamped = Math.Clamp(successesPerClue, 1, 50);
        Campaign campaign = await _dbContext.Campaigns.FindAsync(campaignId)
            ?? throw new InvalidOperationException($"Campaign {campaignId} not found.");

        campaign.SocialManeuverInvestigationSuccessesPerClue = clamped;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Campaign {CampaignId} Social maneuver investigation successes-per-clue set to {Threshold} by ST {UserId}",
            campaignId,
            clamped,
            storytellerUserId);
    }

    /// <inheritdoc />
    public async Task BankInvestigationSuccessesAsync(int maneuverId, int successes, string userId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
        await RequireInitiatorOrStorytellerAsync(maneuver, userId, "bank Investigation successes");

        if (maneuver.Status != ManeuverStatus.Active)
        {
            throw new InvalidOperationException("Investigation successes can only be banked on an active maneuver.");
        }

        if (successes < 1)
        {
            throw new InvalidOperationException("Success count must be at least 1.");
        }

        Campaign campaign = maneuver.Campaign
            ?? await _dbContext.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == maneuver.CampaignId)
            ?? throw new InvalidOperationException($"Campaign {maneuver.CampaignId} not found.");

        (int newProgress, int cluesGranted) = SocialManeuveringEngine.AccrueInvestigationTowardClues(
            maneuver.InvestigationProgressTowardNextClue,
            successes,
            campaign.SocialManeuverInvestigationSuccessesPerClue);

        maneuver.InvestigationProgressTowardNextClue = newProgress;

        for (int i = 0; i < cluesGranted; i++)
        {
            _dbContext.ManeuverClues.Add(new ManeuverClue
            {
                SocialManeuverId = maneuver.Id,
                SourceDescription = "Investigation — successes reached the chronicle clue threshold (Nexus).",
                LeverageKind = ClueLeverageKind.Soft,
            });
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Banked {Successes} Investigation successes on maneuver {ManeuverId}: cluesGranted={Clues}, newProgress={Progress}, user {UserId}",
            successes,
            maneuverId,
            cluesGranted,
            newProgress,
            userId);

        await PublishManeuverUpdateAsync(maneuver.Id);
    }

    /// <inheritdoc />
    public async Task<ManeuverClue> AddManeuverClueAsync(
        int maneuverId,
        string sourceDescription,
        ClueLeverageKind leverageKind,
        string storytellerUserId)
    {
        SocialManeuver maneuver = await LoadManeuverForMutationAsync(maneuverId);
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

        _dbContext.ManeuverClues.Add(clue);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver clue {ClueId} added to maneuver {ManeuverId} by ST {UserId}",
            clue.Id,
            maneuverId,
            storytellerUserId);

        await PublishManeuverUpdateAsync(maneuver.Id);

        return clue;
    }

    /// <inheritdoc />
    public async Task SpendManeuverClueAsync(int clueId, string benefit, string userId)
    {
        ManeuverClue clue = await _dbContext.ManeuverClues
            .Include(c => c.SocialManeuver)
            .ThenInclude(m => m!.TargetNpc)
            .FirstOrDefaultAsync(c => c.Id == clueId)
            ?? throw new InvalidOperationException($"Maneuver clue {clueId} not found.");

        if (clue.SocialManeuver is null)
        {
            throw new InvalidOperationException($"Maneuver clue {clueId} has no maneuver.");
        }

        await RequireInitiatorOrStorytellerAsync(clue.SocialManeuver, userId, "spend a maneuver clue");

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
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Maneuver clue {ClueId} spent on maneuver {ManeuverId} by user {UserId}",
            clueId,
            clue.SocialManeuverId,
            userId);

        await PublishManeuverUpdateAsync(clue.SocialManeuverId);
    }

    private async Task PublishManeuverUpdateAsync(int maneuverId)
    {
        SocialManeuver? row = await _dbContext.SocialManeuvers
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

    private async Task<SocialManeuver> LoadManeuverForMutationAsync(int maneuverId)
    {
        SocialManeuver maneuver = await _dbContext.SocialManeuvers
            .Include(m => m.TargetNpc)
            .Include(m => m.Campaign)
            .FirstOrDefaultAsync(m => m.Id == maneuverId)
            ?? throw new InvalidOperationException($"Social maneuver {maneuverId} not found.");

        await ApplyHostileWeekFailureIfNeededAsync(maneuver, DateTimeOffset.UtcNow);
        return maneuver;
    }

    private async Task ApplyHostileWeekFailureIfNeededAsync(SocialManeuver maneuver, DateTimeOffset nowUtc)
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

        await _dbContext.SaveChangesAsync();

        string stUserId = maneuver.Campaign?.StoryTellerId
            ?? await _dbContext.Campaigns.AsNoTracking()
                .Where(c => c.Id == maneuver.CampaignId)
                .Select(c => c.StoryTellerId)
                .FirstAsync();

        await ApplySocialConditionIfAbsentAsync(
            maneuver.InitiatorCharacterId,
            ConditionType.Shaken,
            "From Social maneuver failure: Hostile impression lasted a week.",
            stUserId);

        await PublishManeuverUpdateAsync(maneuver.Id);
    }

    private async Task RequireInitiatorOrStorytellerAsync(SocialManeuver maneuver, string userId, string operationName)
    {
        Character initiator = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == maneuver.InitiatorCharacterId)
            ?? throw new InvalidOperationException($"Character {maneuver.InitiatorCharacterId} not found.");

        bool isOwner = initiator.ApplicationUserId == userId;
        bool isStoryteller = await _dbContext.Campaigns.AnyAsync(
            c => c.Id == maneuver.CampaignId && c.StoryTellerId == userId);

        if (!isOwner && !isStoryteller)
        {
            _logger.LogWarning(
                "Unauthorized attempt to {OperationName} on maneuver {ManeuverId} by user {UserId}",
                operationName,
                maneuver.Id,
                userId);

            throw new UnauthorizedAccessException(
                $"Only the initiating character's owner or the Storyteller may {operationName}.");
        }
    }

    private async Task ApplyOpenDoorOutcomeConditionsAsync(
        SocialManeuver maneuver,
        RollResult roll,
        int doorsOpened,
        string actingUserId)
    {
        if (maneuver.Status == ManeuverStatus.Succeeded)
        {
            string npcName = maneuver.TargetNpc?.Name ?? "the target";
            await ApplySocialConditionIfAbsentAsync(
                maneuver.InitiatorCharacterId,
                ConditionType.Swooned,
                $"From Social maneuver success toward {npcName}.",
                actingUserId);
            return;
        }

        if (roll.IsDramaticFailure)
        {
            await ApplySocialConditionIfAbsentAsync(
                maneuver.InitiatorCharacterId,
                ConditionType.Shaken,
                "From a dramatic failure while opening a Door in Social maneuvering.",
                actingUserId);
            return;
        }

        if (roll.IsExceptionalSuccess && doorsOpened > 0)
        {
            await ApplySocialConditionIfAbsentAsync(
                maneuver.InitiatorCharacterId,
                ConditionType.Inspired,
                "From an exceptional success while opening a Door in Social maneuvering.",
                actingUserId);
        }
    }

    private async Task ApplySocialConditionIfAbsentAsync(
        int characterId,
        ConditionType type,
        string? descriptionOverride,
        string actingUserId)
    {
        bool hasActive = await _dbContext.CharacterConditions.AnyAsync(
            c => c.CharacterId == characterId && !c.IsResolved && c.ConditionType == type);

        if (hasActive)
        {
            return;
        }

        await _conditionService.ApplyConditionAsync(characterId, type, null, descriptionOverride, actingUserId);
    }
}
