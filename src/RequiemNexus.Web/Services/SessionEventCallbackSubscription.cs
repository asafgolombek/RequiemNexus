namespace RequiemNexus.Web.Services;

/// <summary>
/// Removes a hub-broadcast callback from <see cref="SessionClientService"/> when disposed.
/// </summary>
internal sealed class SessionEventCallbackSubscription(Action remove) : IDisposable
{
    private Action? _remove = remove;

    /// <inheritdoc />
    public void Dispose()
    {
        Interlocked.Exchange(ref _remove, null)?.Invoke();
    }
}
