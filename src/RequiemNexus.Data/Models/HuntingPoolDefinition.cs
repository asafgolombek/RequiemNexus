using System.ComponentModel.DataAnnotations;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Seed row: one entry per PredatorType, containing the canonical hunting pool and Vitae scaling.
/// </summary>
public class HuntingPoolDefinition
{
    [Key]
    public int Id { get; set; }

    public PredatorType PredatorType { get; set; }

    /// <summary>
    /// Serialized <see cref="RequiemNexus.Domain.Models.PoolDefinition"/> (same JSON shape as devotion pools).
    /// </summary>
    [Required]
    public string PoolDefinitionJson { get; set; } = string.Empty;

    /// <summary>Vitae awarded regardless of successes (normally 0).</summary>
    public int BaseVitaeGain { get; set; }

    /// <summary>Vitae awarded per success (normally 1).</summary>
    public int PerSuccessVitaeGain { get; set; } = 1;

    /// <summary>Short narrative description shown in the hunt result UI.</summary>
    [Required]
    [MaxLength(400)]
    public string NarrativeDescription { get; set; } = string.Empty;
}
