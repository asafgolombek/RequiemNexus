namespace RequiemNexus.Application.Services;

/// <summary>
/// Sanitizes user-supplied display names and truncates text for Discord embed limits.
/// </summary>
internal static class DiscordWebhookEmbedText
{
    internal static string SanitizeDisplayName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "Unknown";
        }

        return value.Trim().Replace("@", string.Empty, StringComparison.Ordinal);
    }

    internal static string Truncate(string value, int maxLen)
    {
        if (value.Length <= maxLen)
        {
            return value;
        }

        return value[..(maxLen - 1)] + "…";
    }
}
