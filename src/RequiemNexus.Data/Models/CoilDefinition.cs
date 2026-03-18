using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Defines a single tier within a Scale. Prerequisite chain is explicit (not implicit ordering).
/// ModifiersJson stores JSON-serialized PassiveModifier list for coils with numeric modifiers.
/// </summary>
public class CoilDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Tier within the Scale (1–5).</summary>
    public int Level { get; set; }

    public int ScaleId { get; set; }

    [ForeignKey(nameof(ScaleId))]
    public virtual ScaleDefinition? Scale { get; set; }

    /// <summary>Explicit prerequisite: must hold Coil N-1 before purchasing Coil N. Null for Level 1.</summary>
    public int? PrerequisiteCoilId { get; set; }

    [ForeignKey(nameof(PrerequisiteCoilId))]
    public virtual CoilDefinition? PrerequisiteCoil { get; set; }

    /// <summary>Roll description from source material (e.g., "Resolve + Composure"). Null for passive coils.</summary>
    [MaxLength(200)]
    public string? RollDescription { get; set; }

    /// <summary>
    /// JSON-serialized IReadOnlyList of PassiveModifier for coils providing numeric modifiers.
    /// Null for behavioral/RuleBreaking coils (the majority).
    /// </summary>
    [Column(TypeName = "text")]
    public string? ModifiersJson { get; set; }
}
