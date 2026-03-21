namespace RequiemNexus.Web.Services;

/// <summary>
/// User-visible guidance when the Blazor Server outbound session hub connection fails.
/// </summary>
public static class SessionHubConnectMessages
{
    /// <summary>
    /// Returns a short explanation for UI banners and toasts.
    /// </summary>
    public static string Format(SessionHubConnectResult result) =>
        result switch
        {
            SessionHubConnectResult.FailedMissingCookie =>
                "The server could not read your login cookie for the real-time link. Try a full page refresh (F5).",
            SessionHubConnectResult.FailedNegotiate =>
                "The live session connection was not authenticated. Refresh the page or sign in again.",
            SessionHubConnectResult.ForbiddenNotMember =>
                "You cannot use the live session until you are part of this campaign with a character in the saga.",
            SessionHubConnectResult.RateLimited =>
                "Too many live-session requests. Wait a minute and try again.",
            SessionHubConnectResult.Connected => string.Empty,
            _ => "Could not connect to the live session. Try again or refresh the page.",
        };
}
