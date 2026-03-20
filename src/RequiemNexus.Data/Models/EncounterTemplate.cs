using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Reusable encounter roster (name + NPC rows) for a campaign.
/// </summary>
public class EncounterTemplate
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<EncounterTemplateNpc> Npcs { get; set; } = [];
}
