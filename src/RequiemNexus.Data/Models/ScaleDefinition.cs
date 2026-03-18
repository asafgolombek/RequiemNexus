using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Defines a Scale (thematic Coil grouping) within the Ordo Dracul's Mysteries of the Dragon.
/// Content is seed data; behavior (XP costs, prerequisite chain) is in CoilService.
/// </summary>
public class ScaleDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>The Mystery name this Scale belongs to (e.g., "Mystery of the Ascendant").</summary>
    [MaxLength(100)]
    public string MysteryName { get; set; } = string.Empty;

    /// <summary>Maximum tier for this Scale (5 for all core Scales).</summary>
    public int MaxLevel { get; set; } = 5;

    public virtual ICollection<CoilDefinition> Coils { get; set; } = new List<CoilDefinition>();
}
