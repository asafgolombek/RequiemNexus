using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Professional service purchases (TPT extension).
/// </summary>
public class ServiceAsset : Asset
{
    [MaxLength(80)]
    public string? AssistsSkillName { get; set; }

    public int? DiceBonusMin { get; set; }

    public int? DiceBonusMax { get; set; }

    public int? RecurringResourcesCost { get; set; }
}
