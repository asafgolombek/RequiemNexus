using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Persistent record of a shared dice roll result.
/// Used for play-by-post or sharing results outside of a live session.
/// </summary>
public class PublicRoll
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Short, URL-safe identifier for the roll (e.g., "xK9z2P").
    /// </summary>
    [Required]
    [MaxLength(12)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Optional campaign context for the roll.
    /// </summary>
    public int? CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    /// <summary>
    /// The player who performed the roll.
    /// </summary>
    [Required]
    public string RolledByUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(RolledByUserId))]
    public virtual ApplicationUser? RolledByUser { get; set; }

    /// <summary>
    /// Description of the dice pool (e.g., "Wits + Composure").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PoolDescription { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized DiceRollResultDto.
    /// </summary>
    [Required]
    public string ResultJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
