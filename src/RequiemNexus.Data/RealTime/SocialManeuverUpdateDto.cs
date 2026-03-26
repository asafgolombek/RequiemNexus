using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.RealTime;

/// <summary>
/// Broadcast payload when a Social maneuver changes (Doors, impression, status).
/// </summary>
/// <param name="CampaignId">Chronicle id.</param>
/// <param name="ManeuverId">Primary key of the maneuver.</param>
/// <param name="InitiatorCharacterId">PC initiating the maneuver.</param>
/// <param name="InitiatorCharacterName">Display name of the initiator.</param>
/// <param name="TargetChronicleNpcId">Target NPC id.</param>
/// <param name="TargetNpcName">Display name of the target NPC.</param>
/// <param name="RemainingDoors">Doors left to open.</param>
/// <param name="InitialDoors">Original door count.</param>
/// <param name="CurrentImpression">Current impression level.</param>
/// <param name="Status">Maneuver lifecycle status.</param>
/// <param name="CumulativePenaltyDice">Cumulative failure penalty dice.</param>
/// <param name="LastRollAtUtc">Last open/force roll instant (UTC).</param>
/// <param name="GoalDescription">Stated goal text. Over SignalR campaign broadcast this may be empty for privacy; the Storyteller receives a separate copy with the full text.</param>
public record SocialManeuverUpdateDto(
    int CampaignId,
    int ManeuverId,
    int InitiatorCharacterId,
    string InitiatorCharacterName,
    int TargetChronicleNpcId,
    string TargetNpcName,
    int RemainingDoors,
    int InitialDoors,
    ImpressionLevel CurrentImpression,
    ManeuverStatus Status,
    int CumulativePenaltyDice,
    DateTimeOffset? LastRollAtUtc,
    string GoalDescription);
