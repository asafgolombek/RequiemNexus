using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Additional mechanical role for one owned catalog asset (e.g. tool bonus + weapon profile).
/// </summary>
public class AssetCapability
{
    [Key]
    public int Id { get; set; }

    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public virtual Asset? Asset { get; set; }

    [Required]
    public AssetCapabilityKind Kind { get; set; }

    [MaxLength(80)]
    public string? AssistsSkillName { get; set; }

    public int? DiceBonusMin { get; set; }

    public int? DiceBonusMax { get; set; }

    /// <summary>Target weapon profile asset (not listed in catalog when profile-only).</summary>
    public int? WeaponProfileAssetId { get; set; }

    [ForeignKey(nameof(WeaponProfileAssetId))]
    public virtual Asset? WeaponProfileAsset { get; set; }
}
