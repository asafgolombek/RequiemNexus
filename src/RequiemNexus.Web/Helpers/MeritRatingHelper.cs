namespace RequiemNexus.Web.Helpers;

/// <summary>
/// Parses the bullet-character-based ValidRatings strings from the Merit model
/// into numeric rating values.
/// Formats supported:
///   "••"            → fixed cost of 2 (count the bullets)
///   "• to •••"      → range 1 to 3
///   "•• or ••••"    → discrete options: 2, 4
///   "•, ••, or ••••" → discrete options: 1, 2, 4
/// </summary>
public static class MeritRatingHelper
{
    private const char Bullet = '\u2022'; // •

    /// <summary>
    /// Parses ValidRatings into a sorted list of valid numeric ratings.
    /// </summary>
    /// <param name="validRatings">The string representation of valid ratings (e.g. "•• or ••••").</param>
    public static List<int> ParseValidRatings(string validRatings)
    {
        if (string.IsNullOrWhiteSpace(validRatings))
            return new List<int> { 1 };

        var trimmed = validRatings.Trim();

        // Check for "X to Y" range format (e.g. "• to •••")
        if (trimmed.Contains(" to "))
        {
            var parts = trimmed.Split(" to ", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                int min = CountBullets(parts[0]);
                int max = CountBullets(parts[1]);
                if (min > 0 && max > 0)
                {
                    return Enumerable.Range(min, max - min + 1).ToList();
                }
            }
        }

        // Check for "X or Y" or "X, Y, or Z" format
        // Split by ", " and " or "
        var segments = trimmed
            .Replace(", or ", ", ")
            .Replace(" or ", ", ")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length > 1)
        {
            var ratings = segments
                .Select(CountBullets)
                .Where(v => v > 0)
                .OrderBy(v => v)
                .ToList();
            if (ratings.Count > 0) return ratings;
        }

        // Single value (e.g. "••")
        int single = CountBullets(trimmed);
        if (single > 0)
            return new List<int> { single };

        return new List<int> { 1 };
    }

    /// <summary>
    /// Returns whether this merit has a fixed cost (only one valid rating).
    /// </summary>
    public static bool IsFixedCost(string validRatings)
    {
        return ParseValidRatings(validRatings).Count <= 1;
    }

    /// <param name="validRatings">The string representation of valid ratings (e.g. "•• or ••••").</param>
    /// <returns>The minimum valid rating.</returns>
    public static int GetMinRating(string validRatings)
    {
        var ratings = ParseValidRatings(validRatings);
        return ratings.Count > 0 ? ratings[0] : 1;
    }

    /// <param name="validRatings">The string representation of valid ratings (e.g. "•• or ••••").</param>
    /// <returns>The maximum valid rating.</returns>
    public static int GetMaxRating(string validRatings)
    {
        var ratings = ParseValidRatings(validRatings);
        return ratings.Count > 0 ? ratings[ratings.Count - 1] : 5;
    }

    private static int CountBullets(string s)
    {
        return s.Count(c => c == Bullet);
    }
}
