namespace RequiemNexus.Web.Services;

/// <summary>
/// Outcome of connecting the Blazor Server outbound <see cref="Microsoft.AspNetCore.SignalR.Client.HubConnection"/>
/// to <c>/hubs/session</c> and invoking <c>JoinSession</c>.
/// </summary>
public enum SessionHubConnectResult
{
    /// <summary>Negotiation, transport, and JoinSession succeeded.</summary>
    Connected,

    /// <summary>No Cookie header was available to authenticate the hub (common when HttpContext is missing on the circuit).</summary>
    FailedMissingCookie,

    /// <summary>Negotiate or start failed (e.g. 401/403 from the server).</summary>
    FailedNegotiate,

    /// <summary>Hub returned forbidden — user is not a campaign member for session rules.</summary>
    ForbiddenNotMember,

    /// <summary>Rate limiting blocked negotiate or an invoke.</summary>
    RateLimited,

    /// <summary>Any other failure (including character/chronicle validation on JoinSession).</summary>
    FailedOther,
}
