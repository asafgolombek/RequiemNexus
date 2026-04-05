using RequiemNexus.Application.Contracts;

namespace RequiemNexus.Application.RealTime;

/// <summary>
/// No-op notifier for tests and environments where outbound Discord webhooks are disabled.
/// </summary>
public sealed class NullSessionDiscordNotifier : ISessionDiscordNotifier
{
    /// <inheritdoc />
    public void NotifySessionStarted(int chronicleId, string storytellerDisplayName)
    {
    }

    /// <inheritdoc />
    public void NotifySessionEnded(int chronicleId, string storytellerDisplayName)
    {
    }

    /// <inheritdoc />
    public void NotifyPlayerJoined(int chronicleId, string playerDisplayName)
    {
    }

    /// <inheritdoc />
    public void NotifyPlayerLeft(int chronicleId, string playerUserId)
    {
    }
}
