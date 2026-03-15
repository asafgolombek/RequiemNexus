using Microsoft.AspNetCore.SignalR;
using RequiemNexus.Application.RealTime;

namespace RequiemNexus.Web.Hubs;

/// <summary>
/// Implementation of ISessionPublisher that wraps SignalR IHubContext.
/// Allows the Application layer to broadcast updates without depending on the Web layer.
/// </summary>
public class SessionPublisher(IHubContext<SessionHub, ISessionClient> hubContext) : ISessionPublisher
{
    /// <inheritdoc />
    public ISessionClient Group(int chronicleId)
    {
        return hubContext.Clients.Group(chronicleId.ToString());
    }

    /// <inheritdoc />
    public ISessionClient User(string userId)
    {
        return hubContext.Clients.User(userId);
    }

    /// <inheritdoc />
    public ISessionClient All()
    {
        return hubContext.Clients.All;
    }
}
