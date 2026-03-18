using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Tracks a Coil tier purchased by an Ordo Dracul character. Status flows: Pending → Approved or Rejected.
/// Ordo membership and prerequisite chain enforced by CoilService.
/// </summary>
public class CharacterCoil
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int CoilDefinitionId { get; set; }

    [ForeignKey(nameof(CoilDefinitionId))]
    public virtual CoilDefinition? CoilDefinition { get; set; }

    public CoilLearnStatus Status { get; set; }

    /// <summary>Optional Storyteller note on approval or rejection.</summary>
    [MaxLength(500)]
    public string? StorytellerNote { get; set; }

    /// <summary>When the purchase request was submitted.</summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>When the Storyteller resolved the request (null while Pending).</summary>
    public DateTime? ResolvedAt { get; set; }
}
