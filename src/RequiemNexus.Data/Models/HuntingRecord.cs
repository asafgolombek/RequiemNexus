using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>Audit ledger row written after every hunt attempt.</summary>
public class HuntingRecord
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int? TerritoryId { get; set; }

    [ForeignKey(nameof(TerritoryId))]
    public virtual FeedingTerritory? Territory { get; set; }

    public PredatorType PredatorType { get; set; }

    /// <summary>Human-readable pool description (e.g. "Alleycat: Strength + Brawl, pool 5 dice").</summary>
    [Required]
    [MaxLength(300)]
    public string PoolDescription { get; set; } = string.Empty;

    public int Successes { get; set; }

    public int VitaeGained { get; set; }

    public ResonanceOutcome Resonance { get; set; }

    public DateTime HuntedAt { get; set; } = DateTime.UtcNow;
}
