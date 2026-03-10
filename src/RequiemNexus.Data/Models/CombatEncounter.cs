using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A combat encounter within a campaign. Holds an ordered initiative list of participants.
/// </summary>
public class CombatEncounter
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>True until the ST explicitly resolves the encounter.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public virtual ICollection<InitiativeEntry> InitiativeEntries { get; set; } = [];
}
