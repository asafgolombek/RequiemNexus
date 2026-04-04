namespace RequiemNexus.Application.Services;

/// <summary>
/// Limits concurrent CPU-bound PDF/JSON export work to reduce thread-pool pressure (O-9).
/// </summary>
internal static class CharacterExportConcurrency
{
    private static readonly SemaphoreSlim _gate = new(initialCount: 2, maxCount: 2);

    /// <summary>Runs <paramref name="action"/> after acquiring the export slot.</summary>
    internal static async Task<T> RunThrottledAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
