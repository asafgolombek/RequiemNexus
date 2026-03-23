namespace RequiemNexus.Application.Observability;

/// <summary>
/// Used when <see cref="Microsoft.Extensions.Logging.ILogger.BeginScope{TState}"/> returns null
/// so callers can still use <c>using</c> without nullable analysis warnings.
/// </summary>
internal sealed class NoOpDisposable : IDisposable
{
    /// <summary>Singleton no-op instance.</summary>
    internal static readonly NoOpDisposable Instance = new();

    private NoOpDisposable()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
