using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Immutable record of a single Beat award. Never updated after creation.
/// </summary>
public class BeatLedgerEntry
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int? CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    public BeatSource Source { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>UserId of the Storyteller or system that created this entry. Null for automated sources.</summary>
    [MaxLength(450)]
    public string? AwardedByUserId { get; set; }
}
