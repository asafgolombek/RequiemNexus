using System.Diagnostics;

namespace RequiemNexus.Application.Observability;

/// <summary>
/// Resolves a correlation identifier for logging and tracing. Prefers the current
/// distributed trace id when an <see cref="Activity"/> is active (typical for HTTP and Blazor
/// dispatcher work); otherwise generates a per-operation id so structured logs still correlate.
/// </summary>
public static class AmbientCorrelation
{
    /// <summary>
    /// Returns the W3C trace id from <see cref="Activity.Current"/> when available; otherwise a new id.
    /// Call once per application operation and reuse the value for scopes and log templates.
    /// </summary>
    public static string ForNewOperation()
    {
        Activity? activity = Activity.Current;
        if (activity != null)
        {
            return activity.TraceId.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}
