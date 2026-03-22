using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Armor statistics (TPT extension).
/// </summary>
public class ArmorAsset : Asset
{
    public int ArmorRating { get; set; }

    public int ArmorBallisticRating { get; set; }

    public int ArmorDefenseModifier { get; set; }

    public int ArmorSpeedModifier { get; set; }

    public int Penalty { get; set; }

    public int? StrengthRequirement { get; set; }

    [MaxLength(20)]
    public string? ArmorEra { get; set; }

    [MaxLength(120)]
    public string? ArmorCoverage { get; set; }

    public bool ArmorIsConcealable { get; set; }
}
