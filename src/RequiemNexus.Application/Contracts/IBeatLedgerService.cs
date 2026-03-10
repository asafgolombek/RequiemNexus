using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Records immutable Beat and XP ledger entries and queries the audit trail.
/// All writes produce a new ledger row — existing rows are never mutated.
/// </summary>
public interface IBeatLedgerService
{
    /// <summary>
    /// Appends a Beat-award entry to the ledger.
    /// The caller is responsible for also incrementing <c>Character.Beats</c> and persisting.
    /// </summary>
    /// <param name="characterId">Target character.</param>
    /// <param name="campaignId">Campaign context, if any.</param>
    /// <param name="source">The in-game reason for the Beat.</param>
    /// <param name="reason">Human-readable description.</param>
    /// <param name="awardedByUserId">UserId of the Storyteller or null for automated sources.</param>
    Task RecordBeatAsync(
        int characterId,
        int? campaignId,
        BeatSource source,
        string reason,
        string? awardedByUserId);

    /// <summary>
    /// Appends an XP-credit entry to the ledger (Delta &gt; 0).
    /// The caller is responsible for also updating <c>Character.ExperiencePoints</c> and persisting.
    /// </summary>
    Task RecordXpCreditAsync(
        int characterId,
        int? campaignId,
        int amount,
        XpSource source,
        string reason,
        string? actingUserId);

    /// <summary>
    /// Appends an XP-debit entry to the ledger (Delta &lt; 0).
    /// The caller is responsible for also updating <c>Character.ExperiencePoints</c> and persisting.
    /// </summary>
    Task RecordXpSpendAsync(
        int characterId,
        int? campaignId,
        int cost,
        XpExpense expense,
        string reason,
        string? actingUserId);

    /// <summary>Returns all Beat ledger entries for a character, newest first.</summary>
    Task<List<BeatLedgerEntry>> GetBeatLedgerAsync(int characterId);

    /// <summary>Returns all XP ledger entries for a character, newest first.</summary>
    Task<List<XpLedgerEntry>> GetXpLedgerAsync(int characterId);
}
