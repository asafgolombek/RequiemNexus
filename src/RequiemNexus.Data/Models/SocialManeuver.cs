using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Tracks a VtR 2e Social maneuver: a player character (initiator) working toward a goal against a chronicle NPC.
/// Targets are <see cref="ChronicleNpc"/> only — never another player's PC.
/// </summary>
public class SocialManeuver
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    /// <summary>The PC performing the maneuver; must belong to the same campaign.</summary>
    public int InitiatorCharacterId { get; set; }

    [ForeignKey(nameof(InitiatorCharacterId))]
    public virtual Character? InitiatorCharacter { get; set; }

    /// <summary>The ST-controlled NPC victim.</summary>
    public int TargetChronicleNpcId { get; set; }

    [ForeignKey(nameof(TargetChronicleNpcId))]
    public virtual ChronicleNpc? TargetNpc { get; set; }

    [Required]
    [MaxLength(2000)]
    public string GoalDescription { get; set; } = string.Empty;

    public int InitialDoors { get; set; }

    public int RemainingDoors { get; set; }

    public ImpressionLevel CurrentImpression { get; set; } = ImpressionLevel.Average;

    public ManeuverStatus Status { get; set; } = ManeuverStatus.Active;

    /// <summary>UTC instant of the last attempt to open a Door (for interval enforcement).</summary>
    public DateTimeOffset? LastRollAt { get; set; }

    /// <summary>Book rule: cumulative −1 dice on further rolls with this victim after failures on this maneuver.</summary>
    public int CumulativePenaltyDice { get; set; }

    /// <summary>When impression became Hostile; used to detect one week without improvement.</summary>
    public DateTimeOffset? HostileSince { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual ICollection<ManeuverClue> Clues { get; set; } = new List<ManeuverClue>();
}
