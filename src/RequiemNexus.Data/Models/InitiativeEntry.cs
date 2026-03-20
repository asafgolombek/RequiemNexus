using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// One participant in a <see cref="CombatEncounter"/>.
/// Either a player character (<see cref="CharacterId"/> set) or a named NPC (<see cref="NpcName"/> set).
/// </summary>
public class InitiativeEntry
{
    [Key]
    public int Id { get; set; }

    public int EncounterId { get; set; }

    [ForeignKey(nameof(EncounterId))]
    public virtual CombatEncounter? Encounter { get; set; }

    /// <summary>Set when this entry represents a player character. Null for NPCs.</summary>
    public int? CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    /// <summary>Set when this entry represents an NPC. Null for player characters.</summary>
    [MaxLength(200)]
    public string? NpcName { get; set; }

    /// <summary>Initiative modifier (e.g. Wits + Composure for WoD).</summary>
    public int InitiativeMod { get; set; }

    /// <summary>Result of the initiative die roll (1–10).</summary>
    public int RollResult { get; set; }

    /// <summary>Total initiative = <see cref="InitiativeMod"/> + <see cref="RollResult"/>.</summary>
    public int Total { get; set; }

    /// <summary>True once this participant has taken their action this round.</summary>
    public bool HasActed { get; set; }

    /// <summary>True when this combatant is holding their action until released by the ST.</summary>
    public bool IsHeld { get; set; }

    /// <summary>When false, players see <see cref="MaskedDisplayName"/> or a generic label instead of <see cref="NpcName"/>.</summary>
    public bool IsRevealed { get; set; } = true;

    /// <summary>Optional label shown to players when <see cref="IsRevealed"/> is false (template default or ST override).</summary>
    [MaxLength(200)]
    public string? MaskedDisplayName { get; set; }

    /// <summary>ST-only: size of the NPC health track.</summary>
    public int NpcHealthBoxes { get; set; } = 7;

    /// <summary>ST-only: damage string (same convention as <see cref="Character.HealthDamage"/>).</summary>
    [MaxLength(200)]
    public string NpcHealthDamage { get; set; } = string.Empty;

    /// <summary>Position in turn order (1-indexed, lower = acts sooner).</summary>
    public int Order { get; set; }
}
