using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Audit record of a Predatory Aura (Lash Out) contest between two Kindred.
/// </summary>
public class PredatoryAuraContest
{
    [Key]
    public int Id { get; set; }

    public int ChronicleId { get; set; }

    [ForeignKey(nameof(ChronicleId))]
    public virtual Campaign? Chronicle { get; set; }

    public int AttackerCharacterId { get; set; }

    [ForeignKey(nameof(AttackerCharacterId))]
    public virtual Character? AttackerCharacter { get; set; }

    public int DefenderCharacterId { get; set; }

    [ForeignKey(nameof(DefenderCharacterId))]
    public virtual Character? DefenderCharacter { get; set; }

    public int AttackerBloodPotency { get; set; }

    public int DefenderBloodPotency { get; set; }

    public int AttackerSuccesses { get; set; }

    public int DefenderSuccesses { get; set; }

    /// <summary>Winning character when not a true draw; null when successes and Blood Potency are tied.</summary>
    public int? WinnerId { get; set; }

    [ForeignKey(nameof(WinnerId))]
    public virtual Character? WinnerCharacter { get; set; }

    [Required]
    [MaxLength(50)]
    public string OutcomeApplied { get; set; } = string.Empty;

    public DateTime ResolvedAt { get; set; } = DateTime.UtcNow;

    /// <summary>True for deliberate Lash Out; reserved for future passive first-meeting contests.</summary>
    public bool IsLashOut { get; set; } = true;
}
