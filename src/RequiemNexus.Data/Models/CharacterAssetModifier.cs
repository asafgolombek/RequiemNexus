using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Join table linking an owned item to an applied upgrade.
/// </summary>
public class CharacterAssetModifier
{
    [Key]
    public int Id { get; set; }

    public int CharacterAssetId { get; set; }

    [ForeignKey(nameof(CharacterAssetId))]
    public virtual CharacterAsset? CharacterAsset { get; set; }

    public int AssetModifierId { get; set; }

    [ForeignKey(nameof(AssetModifierId))]
    public virtual AssetModifier? AssetModifier { get; set; }

    /// <summary>Allows a "Sire's Silver Dagger" style override.</summary>
    [MaxLength(200)]
    public string? CustomName { get; set; }
}
