using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Defines a blood sorcery rite (Crúac) or miracle (Theban Sorcery).
/// Content is seed data; behavior is in SorceryService and Unified Pool Resolver.
/// </summary>
public class SorceryRiteDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Rite level (1–5). Character must have matching Discipline dots to learn.</summary>
    public int Level { get; set; }

    /// <summary>Crúac (Circle of the Crone) or Theban Sorcery (Lancea et Sanctum).</summary>
    public SorceryType SorceryType { get; set; }

    /// <summary>XP cost to learn this rite.</summary>
    public int XpCost { get; set; }

    /// <summary>Pool definition JSON for activation rolls. Null for rites with no roll.</summary>
    [Column(TypeName = "text")]
    [MaxLength(2000)]
    public string? PoolDefinitionJson { get; set; }

    /// <summary>Display string for Vitae/Willpower cost (e.g., "1 Vitae", "—").</summary>
    [MaxLength(100)]
    public string? ActivationCostDescription { get; set; }

    /// <summary>Covenant required to learn. Crúac → Circle of the Crone; Theban → Lancea et Sanctum.</summary>
    public int RequiredCovenantId { get; set; }

    [ForeignKey(nameof(RequiredCovenantId))]
    public virtual CovenantDefinition? RequiredCovenant { get; set; }

    /// <summary>Prerequisites text from source material (e.g., "Target must be within a mile.").</summary>
    [MaxLength(500)]
    public string? Prerequisites { get; set; }

    /// <summary>Effect text from source material.</summary>
    public string? Effect { get; set; }
}
