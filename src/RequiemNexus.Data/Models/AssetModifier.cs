using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Global catalog for asset "upgrades" (e.g., Scopes, Silver-plating).
/// </summary>
public class AssetModifier
{
    [Key]
    public int Id { get; set; }

    /// <summary>Stable idempotency key for JSON seeds (e.g. <c>vtm2e:mod:silver-plating</c>). Required — must be unique.</summary>
    [Required]
    [MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>Resources dot threshold (Availability •) to procure/apply.</summary>
    public int Availability { get; set; }

    /// <summary>
    /// JSON blob representing the mechanical changes (following the PassiveModifier/Bonus shape).
    /// </summary>
    public string? ModifierEffectJson { get; set; }

    public virtual ICollection<CharacterAssetModifier> AppliedTo { get; set; } = new List<CharacterAssetModifier>();
}
