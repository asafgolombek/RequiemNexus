using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A third party contesting an active <see cref="SocialManeuver"/>; recorded successes reduce the initiator's net successes on open-Door rolls.
/// </summary>
public class ManeuverInterceptor
{
    [Key]
    public int Id { get; set; }

    public int SocialManeuverId { get; set; }

    [ForeignKey(nameof(SocialManeuverId))]
    public virtual SocialManeuver? SocialManeuver { get; set; }

    public int InterceptorCharacterId { get; set; }

    [ForeignKey(nameof(InterceptorCharacterId))]
    public virtual Character? InterceptorCharacter { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Result of the interceptor's opposition roll (VtR 2e Manipulation + Persuasion contest).</summary>
    public int Successes { get; set; }
}
