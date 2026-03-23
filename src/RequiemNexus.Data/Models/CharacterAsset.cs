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

    /// <summary>Backpack slot 0–9 when carried on person; null when not in backpack. Mutually exclusive with <see cref="IsEquipped"/>.</summary>
    public int? BackpackSlotIndex { get; set; }

    /// <summary>Remaining Structure; null when not tracked. Zero means broken (no bonuses).</summary>
    public int? CurrentStructure { get; set; }

    /// <summary>When true, allows the player to override the display name.</summary>
    public bool IsCustom { get; set; }

    /// <summary>Tracking for the once-per-chapter "Reach" acquisition rule.</summary>
    public DateTimeOffset? LastProcurementDate { get; set; }

    /// <summary>True when this row was acquired via the "Reach" tier (Resources == Availability). Used to scope the once-per-chapter check.</summary>
    public bool WasAcquiredViaReach { get; set; }

    [MaxLength(200)]
    public string? Notes { get; set; }

    public virtual ICollection<CharacterAssetModifier> Modifiers { get; set; } = new List<CharacterAssetModifier>();
}
