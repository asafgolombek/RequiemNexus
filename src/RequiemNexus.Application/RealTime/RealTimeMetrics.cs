using System.Diagnostics.Metrics;

namespace RequiemNexus.Application.RealTime;

/// <summary>
/// Encapsulates OpenTelemetry metrics for the real-time play subsystem.
/// </summary>
public class RealTimeMetrics
{
    private readonly ObservableGauge<long> _activeSessions;
    private readonly ObservableGauge<long> _connectedPlayers;
    private readonly Counter<long> _rollsTotal;
    private readonly Histogram<double> _dispatchDuration;

    private long _activeSessionsCount;
    private long _connectedPlayersCount;

    public RealTimeMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("RequiemNexus.RealTime");

        _activeSessions = meter.CreateObservableGauge("requiem.sessions.active", () => _activeSessionsCount, "sessions", "Number of active play sessions");
        _connectedPlayers = meter.CreateObservableGauge("requiem.sessions.players_connected", () => _connectedPlayersCount, "players", "Number of players connected to all sessions");
        _rollsTotal = meter.CreateCounter<long>("requiem.rolls.broadcast_total", "rolls", "Total number of dice rolls broadcasted");
        _dispatchDuration = meter.CreateHistogram<double>("requiem.hub.dispatch_duration_ms", "ms", "Server-side dispatch duration for hub messages");
    }

    public void SessionStarted() => Interlocked.Increment(ref _activeSessionsCount);

    public void SessionEnded() => Interlocked.Decrement(ref _activeSessionsCount);

    public void PlayerJoined() => Interlocked.Increment(ref _connectedPlayersCount);

    public void PlayerLeft() => Interlocked.Decrement(ref _connectedPlayersCount);

    public void RecordRoll() => _rollsTotal.Add(1);

    public void RecordDispatch(double durationMs) => _dispatchDuration.Record(durationMs);
}
