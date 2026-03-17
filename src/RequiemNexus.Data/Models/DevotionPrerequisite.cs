using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A single discipline prerequisite for a devotion. OR groups use the same OrGroupId — satisfy any group.
/// </summary>
public class DevotionPrerequisite
{
    [Key]
    public int Id { get; set; }

    public int DevotionDefinitionId { get; set; }

    [ForeignKey(nameof(DevotionDefinitionId))]
    public virtual DevotionDefinition? DevotionDefinition { get; set; }

    public int DisciplineId { get; set; }

    [ForeignKey(nameof(DisciplineId))]
    public virtual Discipline? Discipline { get; set; }

    /// <summary>Minimum level required (1-5).</summary>
    public int MinimumLevel { get; set; }

    /// <summary>Prerequisites with the same OrGroupId form an OR group — satisfy any.</summary>
    public int OrGroupId { get; set; }
}
