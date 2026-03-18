using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Seed data defining a bloodline's mechanics. Content is data; behavior is in BloodlineEngine.
/// </summary>
public class BloodlineDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Discipline that becomes the fourth in-clan for characters in this bloodline.</summary>
    public int FourthDisciplineId { get; set; }

    [ForeignKey(nameof(FourthDisciplineId))]
    public virtual Discipline? FourthDiscipline { get; set; }

    /// <summary>Minimum Blood Potency required to join. Default 2.</summary>
    public int PrerequisiteBloodPotency { get; set; } = 2;

    /// <summary>Description of the bloodline's unique Bane. Stacks with parent clan bane.</summary>
    public string BaneOverride { get; set; } = string.Empty;

    /// <summary>When true, mechanics resist clean data modeling; document in rules-interpretations.md.</summary>
    public bool CustomRuleOverride { get; set; }

    /// <summary>Optional description linking to rules-interpretations.md.</summary>
    [MaxLength(500)]
    public string? CustomRuleOverrideDescription { get; set; }

    /// <summary>Clans that can join this bloodline. Supports shared bloodlines (e.g., Vilseduire, Icelus).</summary>
    public virtual ICollection<BloodlineClan> AllowedParentClans { get; set; } = new List<BloodlineClan>();
}
