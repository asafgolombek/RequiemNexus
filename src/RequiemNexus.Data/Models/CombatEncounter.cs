using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A combat encounter within a campaign. Holds an ordered initiative list of participants.
/// </summary>
public class CombatEncounter
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional storyteller-only prep notes for this encounter (draft or reference while running).</summary>
    [MaxLength(4000)]
    public string? PrepNotes { get; set; }

    /// <summary>True for a running fight (launched). Drafts use <see cref="IsDraft"/> and stay inactive until launch.</summary>
    public bool IsActive { get; set; }

    /// <summary>Pre-session prep encounter; cannot be advanced until launched.</summary>
    public bool IsDraft { get; set; }

    /// <summary>True when a launched encounter is paused; initiative stays in the database but live session broadcast is cleared.</summary>
    public bool IsPaused { get; set; }

    /// <summary>Combat round counter (starts at 1 when the encounter is launched).</summary>
    public int CurrentRound { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public virtual ICollection<InitiativeEntry> InitiativeEntries { get; set; } = [];

    public virtual ICollection<EncounterNpcTemplate> NpcTemplates { get; set; } = [];

    /// <summary>Passive Predatory Aura pairs already resolved in this encounter.</summary>
    public virtual ICollection<EncounterAuraContest> EncounterAuraContests { get; set; } = [];
}
