namespace RequiemNexus.Web.Helpers;

/// <summary>
/// Builds short numeric labels for merit <c>ValidRatings</c> strings (bullet notation) for UI copy.
/// </summary>
public static class MeritValidRatingsDisplayFormatter
{
    /// <summary>
    /// Returns a compact label such as "1", "1–5", or "2, 4" derived from parsed valid ratings.
    /// </summary>
    /// <param name="validRatings">Merit.ValidRatings in bullet notation.</param>
    /// <returns>A display string safe for dropdowns and summaries.</returns>
    public static string FormatLabel(string validRatings)
    {
        List<int> ratings = MeritRatingHelper.ParseValidRatings(validRatings);
        if (ratings.Count == 0)
        {
            return "?";
        }

        if (ratings.Count == 1)
        {
            return ratings[0].ToString();
        }

        bool contiguous = true;
        for (int i = 1; i < ratings.Count; i++)
        {
            if (ratings[i] != ratings[i - 1] + 1)
            {
                contiguous = false;
                break;
            }
        }

        if (contiguous)
        {
            return $"{ratings[0]}\u2013{ratings[^1]}";
        }

        return string.Join(", ", ratings);
    }
}
