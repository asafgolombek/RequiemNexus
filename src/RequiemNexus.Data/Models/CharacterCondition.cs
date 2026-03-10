using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A Condition applied to a character. Persists until explicitly resolved.
/// Resolving a Condition that <see cref="AwardsBeat"/> triggers a Beat entry in the ledger.
/// </summary>
public class CharacterCondition
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public ConditionType ConditionType { get; set; }

    /// <summary>Populated when <see cref="ConditionType"/> is <c>Custom</c>.</summary>
    [MaxLength(100)]
    public string? CustomName { get; set; }

    /// <summary>Description override; if null the canonical description is used.</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public bool IsResolved { get; set; }

    /// <summary>Whether resolving this condition awards a Beat (set from <see cref="IConditionRules"/> at creation time).</summary>
    public bool AwardsBeat { get; set; }

    /// <summary>UserId of the Storyteller or player who applied this condition. Null = system.</summary>
    [MaxLength(450)]
    public string? AppliedByUserId { get; set; }
}
