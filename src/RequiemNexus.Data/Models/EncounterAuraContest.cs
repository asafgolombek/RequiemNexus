using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Records that two Kindred have already resolved a passive Predatory Aura contest within a specific combat encounter (deduplication).
/// </summary>
public class EncounterAuraContest
{
    [Key]
    public int Id { get; set; }

    public int EncounterId { get; set; }

    [ForeignKey(nameof(EncounterId))]
    public virtual CombatEncounter? Encounter { get; set; }

    /// <summary>Lower <see cref="Character.Id"/> of the pair (stable ordering).</summary>
    public int VampireLowerId { get; set; }

    [ForeignKey(nameof(VampireLowerId))]
    public virtual Character? VampireLower { get; set; }

    /// <summary>Higher <see cref="Character.Id"/> of the pair (stable ordering).</summary>
    public int VampireHigherId { get; set; }

    [ForeignKey(nameof(VampireHigherId))]
    public virtual Character? VampireHigher { get; set; }

    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;
}
