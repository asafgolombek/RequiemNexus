using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Web.Components.Pages.CharacterSheet;

/// <summary>
/// Display-only helpers for <see cref="SocialManeuversSection"/> (timing copy, clue threshold).
/// </summary>
internal static class SocialManeuverSheetDisplayHelper
{
    /// <summary>Formats a countdown duration for open-door timing hints.</summary>
    public static string FormatDuration(TimeSpan t)
    {
        if (t.TotalDays >= 1)
        {
            return $"{(int)t.TotalDays}d {t.Hours}h {t.Minutes}m";
        }

        if (t.TotalHours >= 1)
        {
            return $"{(int)t.TotalHours}h {t.Minutes}m {t.Seconds}s";
        }

        return $"{t.Minutes}m {t.Seconds}s";
    }

    /// <summary>Player-facing message for when the next open-door roll is allowed.</summary>
    public static string GetTimingMessage(SocialManeuver m)
    {
        if (m.Status != ManeuverStatus.Active)
        {
            return string.Empty;
        }

        TimeSpan? interval = SocialManeuveringEngine.GetMinimumIntervalBetweenOpenDoorRolls(m.CurrentImpression);
        if (interval is null)
        {
            return "Hostile impression — you cannot roll to open Doors.";
        }

        if (m.LastRollAt is null)
        {
            return "You may attempt to open a Door now.";
        }

        DateTimeOffset next = m.LastRollAt.Value + interval.Value;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (now >= next)
        {
            return "You may attempt to open a Door now.";
        }

        TimeSpan remain = next - now;
        return $"Next open-Door roll in {FormatDuration(remain)} (after {next:yyyy-MM-dd HH:mm} UTC).";
    }

    /// <summary>Investigation successes required per clue for this maneuver's campaign.</summary>
    public static int GetClueThreshold(SocialManeuver m) =>
        m.Campaign?.SocialManeuverInvestigationSuccessesPerClue ?? 3;
}
