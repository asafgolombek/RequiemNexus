using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Character's learned rite or pending rite learning request. Status flows: Pending → Approved or Rejected.
/// </summary>
public class CharacterRite
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int SorceryRiteDefinitionId { get; set; }

    [ForeignKey(nameof(SorceryRiteDefinitionId))]
    public virtual SorceryRiteDefinition? SorceryRiteDefinition { get; set; }

    public RiteLearnStatus Status { get; set; }

    /// <summary>Optional note from Storyteller on approval/rejection.</summary>
    [MaxLength(500)]
    public string? StorytellerNote { get; set; }

    /// <summary>When the learning request was submitted.</summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>When the Storyteller approved or rejected (null if still Pending).</summary>
    public DateTime? ResolvedAt { get; set; }
}
