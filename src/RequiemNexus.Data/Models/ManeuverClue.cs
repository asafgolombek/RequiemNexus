using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Optional Nexus extension: an investigation-derived clue spendable toward a <see cref="SocialManeuver"/> (Phase 10.5 behavior).
/// </summary>
public class ManeuverClue
{
    [Key]
    public int Id { get; set; }

    public int SocialManeuverId { get; set; }

    [ForeignKey(nameof(SocialManeuverId))]
    public virtual SocialManeuver? SocialManeuver { get; set; }

    [Required]
    [MaxLength(500)]
    public string SourceDescription { get; set; } = string.Empty;

    public bool IsSpent { get; set; }

    [MaxLength(1000)]
    public string Benefit { get; set; } = string.Empty;

    public ClueLeverageKind LeverageKind { get; set; } = ClueLeverageKind.Soft;
}
