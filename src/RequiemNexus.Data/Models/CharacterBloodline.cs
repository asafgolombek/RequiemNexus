using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Character's bloodline membership or application. Status flows: Pending -> Active or Rejected.
/// </summary>
public class CharacterBloodline
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int BloodlineDefinitionId { get; set; }

    [ForeignKey(nameof(BloodlineDefinitionId))]
    public virtual BloodlineDefinition? BloodlineDefinition { get; set; }

    public BloodlineStatus Status { get; set; } = BloodlineStatus.Pending;

    /// <summary>Optional note from Storyteller on approval/rejection.</summary>
    [MaxLength(500)]
    public string? StorytellerNote { get; set; }

    /// <summary>When the application was submitted.</summary>
    public DateTime AppliedAt { get; set; }

    /// <summary>When the Storyteller approved or rejected (null if still Pending).</summary>
    public DateTime? ResolvedAt { get; set; }
}
