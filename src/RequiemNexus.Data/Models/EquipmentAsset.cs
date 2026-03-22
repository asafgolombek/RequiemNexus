using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

/// <summary>
/// General equipment from <c>generalItems.json</c> (TPT extension).
/// </summary>
public class EquipmentAsset : Asset
{
    /// <summary>Mental / Physical / Social.</summary>
    [MaxLength(20)]
    public string? ItemCategory { get; set; }

    /// <summary>Book skill name (parsed in Application with <c>SkillBookNameParser</c>).</summary>
    [MaxLength(80)]
    public string? AssistsSkillName { get; set; }

    public int? DiceBonusMin { get; set; }

    public int? DiceBonusMax { get; set; }

    public int? ItemSize { get; set; }

    public int? ItemDurability { get; set; }
}
