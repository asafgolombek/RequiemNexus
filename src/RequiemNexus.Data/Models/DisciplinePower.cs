using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class DisciplinePower
{
    [Key]
    public int Id { get; set; }

    public int DisciplineId { get; set; }

    [ForeignKey(nameof(DisciplineId))]
    public virtual Discipline? Discipline { get; set; }

    public int Level { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DicePool { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Cost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized <see cref="RequiemNexus.Domain.Models.PoolDefinition"/> for this power's dice pool.
    /// Uses the same contract as <see cref="DevotionDefinition.PoolDefinitionJson"/>.
    /// Null for powers that have no rollable pool (passive or narrative-only).
    /// Phase 16b reads this column to resolve the dice pool on activation.
    /// </summary>
    public string? PoolDefinitionJson { get; set; }
}
