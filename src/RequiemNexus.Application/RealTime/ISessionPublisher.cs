namespace RequiemNexus.Application.RealTime;

/// <summary>
/// Domain-neutral interface for broadcasting session updates.
/// Implemented in the Web layer using SignalR IHubContext.
/// </summary>
public interface ISessionPublisher
{
    /// <summary>
    /// Gets a client proxy for the entire chronicle group.
    /// </summary>
    ISessionClient Group(int chronicleId);

    /// <summary>
    /// Gets a client proxy for a specific user.
    /// </summary>
    ISessionClient User(string userId);

    /// <summary>
    /// Gets a client proxy for all connected clients.
    /// </summary>
    ISessionClient All();
}
