namespace RequiemNexus.Domain.Models;

/// <summary>
/// Normalization and validation for NPC initiative health tracks (same symbols as character <c>HealthDamage</c>).
/// </summary>
public static class NpcHealthDamageTrack
{
    /// <summary>
    /// Pads or trims the stored value to exactly <paramref name="boxCount"/> characters.
    /// Legacy prefix-only values are preserved on the left; empty slots pad with spaces on the right.
    /// </summary>
    /// <param name="damage">Raw value from persistence (may be short or long).</param>
    /// <param name="boxCount">Number of health boxes on the NPC initiative row.</param>
    /// <returns>A string of length <paramref name="boxCount"/>.</returns>
    public static string Normalize(string? damage, int boxCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(boxCount);

        string s = damage ?? string.Empty;
        if (s.Length > boxCount)
        {
            s = s[..boxCount];
        }

        if (s.Length < boxCount)
        {
            s = s.PadRight(boxCount, ' ');
        }

        return s;
    }

    /// <summary>
    /// Returns true if <paramref name="c"/> is allowed in a full track.
    /// </summary>
    public static bool IsAllowedChar(char c) => c is ' ' or '/' or 'X' or '*';

    /// <summary>
    /// Ensures the track has exactly <paramref name="boxCount"/> characters and only allowed symbols.
    /// </summary>
    /// <exception cref="ArgumentException">When length or charset is invalid.</exception>
    public static void ValidateFullTrack(string damageTrack, int boxCount)
    {
        ArgumentNullException.ThrowIfNull(damageTrack);
        if (damageTrack.Length != boxCount)
        {
            throw new ArgumentException(
                $"Health track must be exactly {boxCount} character(s); got {damageTrack.Length}.",
                nameof(damageTrack));
        }

        foreach (char c in damageTrack)
        {
            if (!IsAllowedChar(c))
            {
                throw new ArgumentException(
                    $"Invalid character '{c}' in health track (use space, '/', 'X', or '*').",
                    nameof(damageTrack));
            }
        }
    }
}
