using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Character-owned inventory row (Phase 11).
/// </summary>
public class CharacterAsset
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

    /// <summary>When true, contributes armor, defense/speed, and dice modifiers.</summary>
    public bool IsEquipped { get; set; } = true;

    /// <summary>Quick-slot index 0–2 for Pack UI; null when not pinned.</summary>
    public int? ReadySlotIndex { get; set; }

    /// <summary>Remaining Structure; null when not tracked. Zero means broken (no bonuses).</summary>
    public int? CurrentStructure { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }
}
