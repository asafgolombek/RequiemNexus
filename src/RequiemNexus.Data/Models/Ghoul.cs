using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A ghoul retainer tracked by the Storyteller (not a full <see cref="Character"/> sheet).
/// </summary>
public class Ghoul
{
    [Key]
    public int Id { get; set; }

    public int ChronicleId { get; set; }

    [ForeignKey(nameof(ChronicleId))]
    public virtual Campaign? Chronicle { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public int? RegnantCharacterId { get; set; }

    [ForeignKey(nameof(RegnantCharacterId))]
    public virtual Character? RegnantCharacter { get; set; }

    public int? RegnantNpcId { get; set; }

    [ForeignKey(nameof(RegnantNpcId))]
    public virtual ChronicleNpc? RegnantNpc { get; set; }

    [MaxLength(150)]
    public string? RegnantDisplayName { get; set; }

    public DateTime? LastFedAt { get; set; }

    /// <summary>Vitae held in the ghoul's system (0 or 1 for mortals).</summary>
    public int VitaeInSystem { get; set; }

    public int? ApparentAge { get; set; }

    public int? ActualAge { get; set; }

    /// <summary>JSON array of discipline IDs accessible at rating 1.</summary>
    [MaxLength(2000)]
    public string? AccessibleDisciplinesJson { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public bool IsReleased { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
