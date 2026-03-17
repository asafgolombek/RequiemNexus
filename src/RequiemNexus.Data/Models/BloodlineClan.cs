using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Join table: BloodlineDefinition to allowed parent Clan. Supports shared bloodlines.
/// </summary>
public class BloodlineClan
{
    [Key]
    public int Id { get; set; }

    public int BloodlineDefinitionId { get; set; }

    [ForeignKey(nameof(BloodlineDefinitionId))]
    public virtual BloodlineDefinition? BloodlineDefinition { get; set; }

    public int ClanId { get; set; }

    [ForeignKey(nameof(ClanId))]
    public virtual Clan? Clan { get; set; }
}
