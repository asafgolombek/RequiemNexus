using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application orchestration for VtR 2e Social maneuvering (Doors, impression timing, forcing).
/// </summary>
public interface ISocialManeuveringService
{
    /// <summary>
    /// Creates a maneuver. Storyteller-only; target must be a chronicle NPC in the same campaign as the initiator.
    /// </summary>
    Task<SocialManeuver> CreateAsync(
        int campaignId,
        int initiatorCharacterId,
        int targetChronicleNpcId,
        string goalDescription,
        bool goalWouldBeBreakingPoint,
        bool goalPreventsAspiration,
        bool actsAgainstVirtueOrMask,
        string storytellerUserId);

    /// <summary>
    /// Sets impression (ST narrative control). Tracks <see cref="SocialManeuver.HostileSince"/> when entering Hostile.
    /// </summary>
    Task SetImpressionAsync(int maneuverId, ImpressionLevel impression, string storytellerUserId);

    /// <summary>
    /// Sets remaining Doors for narrative adjustment. Storyteller-only; clamped to [0, <see cref="SocialManeuver.InitialDoors"/>].
    /// </summary>
    Task SetRemainingDoorsNarrativeAsync(int maneuverId, int remainingDoors, string storytellerUserId);

    /// <summary>
    /// Storyteller-only: sets how many Investigation successes are required to create one <see cref="ManeuverClue"/> when banking (clamped 1–50).
    /// </summary>
    Task SetInvestigationSuccessesPerClueAsync(int campaignId, int successesPerClue, string storytellerUserId);

    /// <summary>
    /// Banks Investigation successes toward automatic clues for this maneuver. Initiator owner or Storyteller; maneuver must be <see cref="ManeuverStatus.Active"/>.
    /// </summary>
    Task BankInvestigationSuccessesAsync(int maneuverId, int successes, string userId);

    /// <summary>
    /// Storyteller-only: adds a maneuver clue (manual ST grant).
    /// </summary>
    Task<ManeuverClue> AddManeuverClueAsync(
        int maneuverId,
        string sourceDescription,
        ClueLeverageKind leverageKind,
        string storytellerUserId);

    /// <summary>
    /// Spends an unspent clue (ST or initiator). Records mechanical benefit text; publishes update.
    /// </summary>
    Task SpendManeuverClueAsync(int clueId, string benefit, string userId);

    /// <summary>
    /// Storyteller-only: adds a character as an interceptor on an active maneuver (unique per maneuver + character).
    /// </summary>
    Task<ManeuverInterceptor> AddInterceptorAsync(int socialManeuverId, int interceptorCharacterId, string storytellerUserId);

    /// <summary>
    /// Storyteller-only: records the interceptor's opposition successes (Manipulation + Persuasion contest); capped by the interceptor's sheet pool.
    /// </summary>
    Task RecordInterceptorRollAsync(int interceptorId, int successes, string storytellerUserId);
}
