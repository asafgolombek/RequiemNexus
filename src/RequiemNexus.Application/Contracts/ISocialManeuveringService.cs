using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

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
    /// Lists all maneuvers in a campaign. Storyteller-only.
    /// </summary>
    Task<IReadOnlyList<SocialManeuver>> ListForCampaignAsync(int campaignId, string storytellerUserId);

    /// <summary>
    /// Lists maneuvers initiated by the character. Character owner or Storyteller may read.
    /// </summary>
    Task<IReadOnlyList<SocialManeuver>> ListForInitiatorAsync(int characterId, string userId);

    /// <summary>
    /// Rolls to open one or two Doors (per book). Initiator owner or Storyteller; enforces impression interval and cumulative failure dice.
    /// </summary>
    Task<(SocialManeuver Updated, RollResult Roll, int DoorsOpened)> RollOpenDoorAsync(
        int maneuverId,
        int dicePool,
        string userId);

    /// <summary>
    /// Forces remaining Doors with pool penalty equal to closed Doors. Optional hard leverage removes Doors first per Humanity gap.
    /// </summary>
    Task<(SocialManeuver Updated, RollResult Roll, bool ForcedSuccess)> RollForceDoorsAsync(
        int maneuverId,
        int dicePool,
        bool applyHardLeverage,
        int breakingPointSeverity,
        string userId);

    /// <summary>
    /// Sets impression (ST narrative control). Tracks <see cref="SocialManeuver.HostileSince"/> when entering Hostile.
    /// </summary>
    Task SetImpressionAsync(int maneuverId, ImpressionLevel impression, string storytellerUserId);

    /// <summary>
    /// Sets remaining Doors for narrative adjustment. Storyteller-only; clamped to [0, <see cref="SocialManeuver.InitialDoors"/>].
    /// </summary>
    Task SetRemainingDoorsNarrativeAsync(int maneuverId, int remainingDoors, string storytellerUserId);
}
