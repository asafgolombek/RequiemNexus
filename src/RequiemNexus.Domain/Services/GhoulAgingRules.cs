namespace RequiemNexus.Domain.Services;

/// <summary>
/// Stateless aging rules for ghouls per V:tR 2e p. 210.
/// </summary>
public static class GhoulAgingRules
{
    /// <summary>
    /// The maximum time a ghoul can go without Vitae before aging begins.
    /// Interpretation: fixed 30-day interval (see rules-interpretations.md Phase 12).
    /// </summary>
    public static readonly TimeSpan FeedingInterval = TimeSpan.FromDays(30);

    /// <summary>
    /// Returns true when the ghoul is overdue for feeding and aging damage is pending.
    /// </summary>
    /// <param name="lastFedAt">Last Vitae feeding instant, if recorded.</param>
    /// <param name="now">Current instant (UTC).</param>
    /// <returns>True when never fed or elapsed time strictly exceeds <see cref="FeedingInterval"/>.</returns>
    public static bool IsAgingDue(DateTime? lastFedAt, DateTime now) =>
        lastFedAt is null || (now - lastFedAt.Value) > FeedingInterval;

    /// <summary>
    /// Returns the number of full months the ghoul has gone without Vitae after the feeding grace period.
    /// Each full overdue month represents one potential lethal damage level
    /// equal to (ActualAge − ApparentAge) if tracked, otherwise 1 per month.
    /// The ST determines and applies the actual damage.
    /// </summary>
    /// <param name="lastFedAt">Last Vitae feeding instant (UTC).</param>
    /// <param name="now">Current instant (UTC).</param>
    /// <returns>Zero when still within or at the grace period; otherwise full 30-day periods after grace.</returns>
    public static int OverdueMonths(DateTime lastFedAt, DateTime now)
    {
        TimeSpan elapsed = now - lastFedAt;
        if (elapsed <= FeedingInterval)
        {
            return 0;
        }

        return (int)((elapsed - FeedingInterval).TotalDays / 30);
    }
}
