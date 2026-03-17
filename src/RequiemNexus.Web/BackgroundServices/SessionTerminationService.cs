using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.RealTime;
using StackExchange.Redis;

namespace RequiemNexus.Web.BackgroundServices;

/// <summary>
/// Background service that watches for session expiry in Redis.
/// When the Storyteller's heartbeat expires (15 min), this service cleans up the session state
/// and notifies all connected players.
/// </summary>
public class SessionTerminationService(
    IConnectionMultiplexer redis,
    ISessionPublisher publisher,
    ISessionStateRepository repository,
    RealTimeMetrics metrics,
    ILogger<SessionTerminationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Session Termination Service is starting.");

        var subscriber = redis.GetSubscriber();

        // Subscribe to keyspace notifications for key expiry.
        // Requires 'notify-keyspace-events Ex' to be enabled on the Redis server.
        // We target db 0 by default.
        var channel = new RedisChannel("__keyevent@0__:expired", RedisChannel.PatternMode.Literal);

        await subscriber.SubscribeAsync(channel, async (_, message) =>
        {
            var key = (string)message!;

            // We look for the 'session:{chronicleId}:info' key expiry
            if (key.StartsWith("session:") && key.EndsWith(":info"))
            {
                var segments = key.Split(':');
                if (segments.Length == 3 && int.TryParse(segments[1], out var chronicleId))
                {
                    logger.LogInformation("Session for chronicle {ChronicleId} expired. Cleaning up sibling keys.", chronicleId);

                    // 1. Notify all players in the group
                    try
                    {
                        await publisher.Group(chronicleId).SessionEnded("Session auto-terminated due to Storyteller disconnect.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to broadcast SessionEnded for chronicle {ChronicleId}.", chronicleId);
                    }

                    // 2. Clean up rolls, presence, and initiative (the info key is already gone)
                    try
                    {
                        await repository.DeleteSessionAsync(chronicleId);
                        metrics.SessionEnded();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to clean up Redis state for expired chronicle {ChronicleId}.", chronicleId);
                    }
                }
            }
        });

        // Wait until the service is stopped
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Session Termination Service is stopping.");
        }
    }
}
