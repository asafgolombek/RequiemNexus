using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A Tilt applied to a character — a transient combat state that lasts for a scene.
/// Tilts do not award Beats when removed.
/// </summary>
public class CharacterTilt
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    /// <summary>Optional link to the encounter in which this Tilt was applied.</summary>
    public int? EncounterId { get; set; }

    public TiltType TiltType { get; set; }

    /// <summary>Populated when <see cref="TiltType"/> is <c>Custom</c>.</summary>
    [MaxLength(100)]
    public string? CustomName { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public DateTime? RemovedAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>UserId of the Storyteller or player who applied this Tilt.</summary>
    [MaxLength(450)]
    public string? AppliedByUserId { get; set; }
}
