namespace RequiemNexus.Domain.Services;

/// <summary>
/// Minimum time in torpor before hunger escalation milestones (VtR 2e p. 165), keyed by Blood Potency (1–10).
/// </summary>
public static class TorporDurationTable
{
    /// <summary>
    /// Blood Potency → minimum torpor length in days (VtR 2e p. 165).
    /// BP 10 (&quot;indefinitely&quot;) uses <see cref="int.MaxValue"/>; treated as no automatic starvation notification threshold.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, int> MinimumDaysById = new Dictionary<int, int>
    {
        { 1, 1 },
        { 2, 7 },
        { 3, 30 },
        { 4, 365 },
        { 5, 3_650 },
        { 6, 36_500 },
        { 7, 182_500 },
        { 8, 365_000 },
        { 9, 3_650_000 },
        { 10, int.MaxValue },
    };

    /// <summary>
    /// Returns the minimum days for the given Blood Potency, defaulting to BP 1 when out of range.
    /// </summary>
    public static int GetMinimumDays(int bloodPotency)
    {
        if (MinimumDaysById.TryGetValue(bloodPotency, out int days))
        {
            return days;
        }

        return MinimumDaysById[1];
    }
}
