namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Phase 20: Optional Discord incoming webhook posts for live session presence. Implementations must not block hub dispatch.
/// </summary>
public interface ISessionDiscordNotifier
{
    /// <summary>Posts a non-blocking session-started notification when a webhook is configured for the chronicle.</summary>
    /// <param name="chronicleId">Campaign / chronicle id.</param>
    /// <param name="storytellerDisplayName">Display name for the ST (sanitized in the notifier).</param>
    void NotifySessionStarted(int chronicleId, string storytellerDisplayName);

    /// <summary>Posts a non-blocking session-ended notification when a webhook is configured.</summary>
    void NotifySessionEnded(int chronicleId, string storytellerDisplayName);

    /// <summary>Posts a non-blocking join notification when a webhook is configured.</summary>
    void NotifyPlayerJoined(int chronicleId, string playerDisplayName);

    /// <summary>Posts a non-blocking leave notification when a webhook is configured.</summary>
    /// <param name="chronicleId">Campaign / chronicle id.</param>
    /// <param name="playerUserId">AspNetUsers id of the disconnected player.</param>
    void NotifyPlayerLeft(int chronicleId, string playerUserId);
}
