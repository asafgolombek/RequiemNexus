using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A Blood Bond (Vinculum) between a thrall character and a regnant (PC, NPC, or display-name only).
/// </summary>
public class BloodBond
{
    [Key]
    public int Id { get; set; }

    public int ChronicleId { get; set; }

    [ForeignKey(nameof(ChronicleId))]
    public virtual Campaign? Chronicle { get; set; }

    public int ThrallCharacterId { get; set; }

    [ForeignKey(nameof(ThrallCharacterId))]
    public virtual Character? ThrallCharacter { get; set; }

    public int? RegnantCharacterId { get; set; }

    [ForeignKey(nameof(RegnantCharacterId))]
    public virtual Character? RegnantCharacter { get; set; }

    public int? RegnantNpcId { get; set; }

    [ForeignKey(nameof(RegnantNpcId))]
    public virtual ChronicleNpc? RegnantNpc { get; set; }

    [MaxLength(150)]
    public string? RegnantDisplayName { get; set; }

    /// <summary>
    /// Synthetic uniqueness key set by the Application layer (e.g. <c>c:12</c>, <c>n:3</c>, <c>d:name</c>).
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string RegnantKey { get; set; } = string.Empty;

    /// <summary>Bond stage from 1 to 3.</summary>
    public int Stage { get; set; } = 1;

    public DateTime? LastFedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
