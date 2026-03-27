using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Applies and heals damage on the CoD-style health string (left-to-right fill, bashing overflow rules).
/// </summary>
public static class HealthTrackMutator
{
    /// <summary>
    /// Pads or truncates a track to <paramref name="maxHealth"/> boxes; unknown characters become spaces.
    /// </summary>
    /// <param name="track">Existing damage string; may be null.</param>
    /// <param name="maxHealth">Desired length.</param>
    public static string NormalizeTrack(string? track, int maxHealth)
    {
        if (maxHealth <= 0)
        {
            return string.Empty;
        }

        string s = track ?? string.Empty;
        if (s.Length > maxHealth)
        {
            s = s[..maxHealth];
        }

        Span<char> buffer = stackalloc char[maxHealth];
        for (int i = 0; i < maxHealth; i++)
        {
            if (i < s.Length)
            {
                char c = s[i];
                buffer[i] = c is '/' or 'X' or '*' ? c : ' ';
            }
            else
            {
                buffer[i] = ' ';
            }
        }

        return new string(buffer);
    }

    /// <summary>
    /// Counts boxes that currently hold damage.
    /// </summary>
    public static int CountDamagedBoxes(string? track, int maxHealth)
    {
        string t = NormalizeTrack(track, maxHealth);
        int n = 0;
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] != ' ')
            {
                n++;
            }
        }

        return n;
    }

    /// <summary>
    /// Applies <paramref name="instances"/> hits of <paramref name="kind"/> using overflow rules when the track is full.
    /// </summary>
    public static string ApplyDamage(string? track, int maxHealth, HealthDamageKind kind, int instances)
    {
        string current = NormalizeTrack(track, maxHealth);
        for (int i = 0; i < instances; i++)
        {
            current = ApplySingleHit(current, maxHealth, kind);
        }

        return current;
    }

    /// <summary>
    /// Removes the rightmost bashing box (VtR fast-heal convention).
    /// </summary>
    public static string HealRightmostBashing(string? track, int maxHealth)
    {
        char[] boxes = NormalizeTrack(track, maxHealth).ToCharArray();
        for (int i = boxes.Length - 1; i >= 0; i--)
        {
            if (boxes[i] == '/')
            {
                boxes[i] = ' ';
                break;
            }
        }

        return new string(boxes);
    }

    private static string ApplySingleHit(string track, int maxHealth, HealthDamageKind kind)
    {
        char[] boxes = NormalizeTrack(track, maxHealth).ToCharArray();
        char symbol = kind.ToTrackSymbol();

        EnsureEmptyBox(boxes);
        int empty = IndexOfFirstSpace(boxes);
        if (empty >= 0)
        {
            boxes[empty] = symbol;
        }

        return new string(boxes);
    }

    private static void EnsureEmptyBox(char[] boxes)
    {
        if (IndexOfFirstSpace(boxes) >= 0)
        {
            return;
        }

        while (IndexOfFirstSpace(boxes) < 0)
        {
            int rb = LastIndexOf(boxes, '/');
            if (rb >= 0)
            {
                boxes[rb] = 'X';
                continue;
            }

            int rl = LastIndexOf(boxes, 'X');
            if (rl >= 0)
            {
                boxes[rl] = '*';
                continue;
            }

            break;
        }
    }

    private static int IndexOfFirstSpace(char[] boxes)
    {
        for (int i = 0; i < boxes.Length; i++)
        {
            if (boxes[i] == ' ')
            {
                return i;
            }
        }

        return -1;
    }

    private static int LastIndexOf(char[] boxes, char c)
    {
        for (int i = boxes.Length - 1; i >= 0; i--)
        {
            if (boxes[i] == c)
            {
                return i;
            }
        }

        return -1;
    }
}
