using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// NPC row on a draft <see cref="CombatEncounter"/>; rolled into <see cref="InitiativeEntry"/> when the ST launches the fight.
/// </summary>
public class EncounterNpcTemplate
{
    [Key]
    public int Id { get; set; }

    public int EncounterId { get; set; }

    [ForeignKey(nameof(EncounterId))]
    public virtual CombatEncounter? Encounter { get; set; }

    /// <summary>When set, this row was added from Danse Macabre; at most one per encounter.</summary>
    public int? ChronicleNpcId { get; set; }

    [ForeignKey(nameof(ChronicleNpcId))]
    public virtual ChronicleNpc? ChronicleNpc { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public int InitiativeMod { get; set; }

    public int HealthBoxes { get; set; } = 7;

    /// <summary>Maximum willpower dots for this NPC at encounter launch.</summary>
    public int MaxWillpower { get; set; } = 4;

    /// <summary>Maximum vitae for Kindred NPCs at launch; 0 when not applicable.</summary>
    public int MaxVitae { get; set; }

    /// <summary>ST-only reference notes.</summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>When false at launch, initiative entry uses <see cref="DefaultMaskedName"/> for player-facing display.</summary>
    public bool IsRevealed { get; set; } = true;

    /// <summary>Default mask when <see cref="IsRevealed"/> is false (e.g. &quot;Unknown assailant&quot;).</summary>
    [MaxLength(200)]
    public string? DefaultMaskedName { get; set; }

    /// <summary>Equivalent to <see cref="DefaultMaskedName"/>; matches <see cref="InitiativeEntry.MaskedDisplayName"/>.</summary>
    [MaxLength(200)]
    public string? MaskedDisplayName { get; set; }
}
