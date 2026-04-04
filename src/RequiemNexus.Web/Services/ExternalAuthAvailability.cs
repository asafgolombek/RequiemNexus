using Microsoft.Extensions.Configuration;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Reports whether Google and Discord OAuth are configured with real client credentials (not placeholder values).
/// Used to hide external login UI when providers are not set up on the host.
/// </summary>
public sealed class ExternalAuthAvailability
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalAuthAvailability"/> class.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    public ExternalAuthAvailability(IConfiguration configuration) =>
        _configuration = configuration;

    /// <summary>
    /// Gets a value indicating whether Google OAuth client id and secret are configured.
    /// </summary>
    public bool IsGoogleEnabled => IsPairConfigured("Authentication:Google:ClientId", "Authentication:Google:ClientSecret");

    /// <summary>
    /// Gets a value indicating whether Discord OAuth client id and secret are configured.
    /// </summary>
    public bool IsDiscordEnabled => IsPairConfigured("Authentication:Discord:ClientId", "Authentication:Discord:ClientSecret");

    private bool IsPairConfigured(string clientIdKey, string clientSecretKey)
    {
        static bool IsRealCredential(string? value) =>
            !string.IsNullOrWhiteSpace(value)
            && !string.Equals(value, "not-configured", StringComparison.OrdinalIgnoreCase);

        return IsRealCredential(_configuration[clientIdKey]) && IsRealCredential(_configuration[clientSecretKey]);
    }
}
