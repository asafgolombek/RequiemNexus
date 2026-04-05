using RequiemNexus.Domain.Models;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Validates Discord incoming webhook URLs before persistence (Phase 20 — coterie status channel).
/// </summary>
public static class DiscordIncomingWebhookValidator
{
    private const int _maxLength = 512;

    /// <summary>
    /// Validates and returns a trimmed URL, or <c>null</c> when <paramref name="rawUrl"/> is empty (clear webhook).
    /// </summary>
    public static Result<string?> Validate(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return Result<string?>.Success(null);
        }

        string trimmed = rawUrl.Trim();
        if (trimmed.Length > _maxLength)
        {
            return Result<string?>.Failure("Webhook URL is too long.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
        {
            return Result<string?>.Failure("Webhook URL must be a valid absolute URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return Result<string?>.Failure("Webhook URL must use HTTPS.");
        }

        string host = uri.Host;
        if (!host.Equals("discord.com", StringComparison.OrdinalIgnoreCase)
            && !host.Equals("discordapp.com", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string?>.Failure("Webhook URL must use discord.com (or discordapp.com).");
        }

        if (!uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string?>.Failure("URL must be a Discord incoming webhook (/api/webhooks/...).");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            return Result<string?>.Failure("Webhook URL must not contain user credentials.");
        }

        return Result<string?>.Success(trimmed);
    }
}
