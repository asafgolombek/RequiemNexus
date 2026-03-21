using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// One NPC line on an <see cref="EncounterTemplate"/>.
/// </summary>
public class EncounterTemplateNpc
{
    [Key]
    public int Id { get; set; }

    public int TemplateId { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public virtual EncounterTemplate? Template { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int InitiativeMod { get; set; }

    public int HealthBoxes { get; set; } = 7;

    /// <summary>Maximum willpower dots when this template line is copied to a draft encounter.</summary>
    public int MaxWillpower { get; set; } = 4;

    public bool IsRevealedByDefault { get; set; } = true;

    [MaxLength(200)]
    public string? DefaultMaskedName { get; set; }
}
