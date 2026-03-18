using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Seed data defining a devotion's mechanics. Content is data; behavior is in TraitResolver + DiceService.
/// </summary>
public class DevotionDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>XP cost to learn this devotion.</summary>
    public int XpCost { get; set; }

    /// <summary>Pool definition JSON for additive dice pools. Null/empty for passive or no-roll devotions.</summary>
    [Column(TypeName = "text")]
    [MaxLength(2000)]
    public string? PoolDefinitionJson { get; set; }

    /// <summary>When true, this is a passive devotion (display-only in Phase 8; modifier engine deferred to Phase 9).</summary>
    public bool IsPassive { get; set; }

    /// <summary>Display string for activation cost (e.g., "●●", "●(○)", "—").</summary>
    [MaxLength(50)]
    public string? ActivationCostDescription { get; set; }

    /// <summary>When set, only characters in this bloodline can learn the devotion.</summary>
    public int? RequiredBloodlineId { get; set; }

    [ForeignKey(nameof(RequiredBloodlineId))]
    public virtual BloodlineDefinition? RequiredBloodline { get; set; }

    /// <summary>Source citation (e.g., "VTR 2e 142").</summary>
    [MaxLength(50)]
    public string? Source { get; set; }

    public virtual ICollection<DevotionPrerequisite> Prerequisites { get; set; } = new List<DevotionPrerequisite>();
}
