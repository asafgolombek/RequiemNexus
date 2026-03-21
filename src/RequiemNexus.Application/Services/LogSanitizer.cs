namespace RequiemNexus.Application.Services;

/// <summary>
/// Removes CR/LF from strings before they are written to log templates, mitigating log forging.
/// </summary>
internal static class LogSanitizer
{
    /// <summary>
    /// Returns a copy of <paramref name="value"/> with carriage return and line feed replaced by spaces.
    /// </summary>
    public static string ForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        return value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
