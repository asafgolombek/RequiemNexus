using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace RequiemNexus.Web.Hubs.Filters;

/// <summary>
/// A SignalR Hub Filter that enforces simple rate limiting per connection.
/// This prevents clients from flooding the hub with messages.
/// </summary>
public class RateLimitingFilter(int maxMessagesPerMinute) : IHubFilter
{
    private readonly ConcurrentDictionary<string, ConnectionStats> _stats = new();
    private readonly int _maxMessagesPerMinute = maxMessagesPerMinute;

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var connectionId = invocationContext.Context.ConnectionId;
        var now = DateTime.UtcNow;

        var stats = _stats.GetOrAdd(connectionId, _ => new ConnectionStats { WindowStart = now });

        lock (stats)
        {
            if (now - stats.WindowStart > TimeSpan.FromMinutes(1))
            {
                stats.WindowStart = now;
                stats.MessageCount = 0;
            }

            stats.MessageCount++;

            if (stats.MessageCount > _maxMessagesPerMinute)
            {
                throw new HubException($"Too many requests. The Masquerade requires patience. (Limit: {_maxMessagesPerMinute}/min)");
            }
        }

        return await next(invocationContext);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        _stats.TryRemove(context.Context.ConnectionId, out _);
        return next(context, exception);
    }

    private class ConnectionStats
    {
        public DateTime WindowStart { get; set; }
        public int MessageCount { get; set; }
    }
}
