using RequiemNexus.Web.Dtos;
using RequiemNexus.Web.Enums;

namespace RequiemNexus.Web.Services;

public sealed class ToastService : IDisposable
{
    private readonly List<ToastItem> _toasts = new();
    private readonly Dictionary<Guid, System.Timers.Timer> _timers = new();

    public event Action? OnToastsChanged;

    public IReadOnlyList<ToastItem> Toasts
    {
        get
        {
            lock (_toasts)
            {
                return _toasts.AsReadOnly();
            }
        }
    }

    public void Show(string title, string message, ToastType type, int durationMs = 3000)
    {
        var id = Guid.NewGuid();
        var toast = new ToastItem(id, title, message, type, durationMs, DateTime.UtcNow);

        lock (_toasts)
        {
            if (_toasts.Count >= 4)
            {
                var oldest = _toasts[0];
                RemoveToast(oldest.Id);
            }

            _toasts.Add(toast);
        }

        var timer = new System.Timers.Timer(durationMs);
        timer.Elapsed += (s, e) => RemoveToast(id);
        timer.AutoReset = false;
        timer.Enabled = true;

        lock (_timers)
        {
            _timers[id] = timer;
        }

        OnToastsChanged?.Invoke();
    }

    /// <summary>Dismisses a toast immediately (e.g. user clicked dismiss).</summary>
    public void Dismiss(Guid id) => RemoveToast(id);

    public void Dispose()
    {
        lock (_timers)
        {
            foreach (var timer in _timers.Values)
            {
                timer.Dispose();
            }

            _timers.Clear();
        }
    }

    private void RemoveToast(Guid id)
    {
        lock (_toasts)
        {
            var toast = _toasts.Find(t => t.Id == id);
            if (toast != null)
            {
                _toasts.Remove(toast);
            }
        }

        lock (_timers)
        {
            if (_timers.TryGetValue(id, out var timer))
            {
                timer.Dispose();
                _timers.Remove(id);
            }
        }

        OnToastsChanged?.Invoke();
    }
}
