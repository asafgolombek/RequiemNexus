using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Immutable record of a single XP credit or debit. Never updated after creation.
/// Positive <see cref="Delta"/> = XP earned; negative = XP spent.
/// </summary>
public class XpLedgerEntry
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int? CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    /// <summary>Positive = XP earned, negative = XP spent.</summary>
    public int Delta { get; set; }

    /// <summary>Set when this entry represents an XP credit (Delta &gt; 0).</summary>
    public XpSource? Source { get; set; }

    /// <summary>Set when this entry represents an XP expense (Delta &lt; 0).</summary>
    public XpExpense? Expense { get; set; }

    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>UserId of the actor who triggered this entry. Null for automated conversions.</summary>
    [MaxLength(450)]
    public string? ActingUserId { get; set; }
}
