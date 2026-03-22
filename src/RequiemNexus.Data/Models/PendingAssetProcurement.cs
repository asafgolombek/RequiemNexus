using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Illicit or ST-gated procurement awaiting Storyteller approval.
/// </summary>
public class PendingAssetProcurement
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }

    public int Quantity { get; set; } = 1;

    [Required]
    public PendingAssetProcurementStatus Status { get; set; } = PendingAssetProcurementStatus.Pending;

    /// <summary>Player narrative for the request (optional).</summary>
    [MaxLength(2000)]
    public string? PlayerNote { get; set; }

    /// <summary>ST rejection or approval note.</summary>
    [MaxLength(2000)]
    public string? StorytellerNote { get; set; }

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ResolvedAt { get; set; }
}
