using Microsoft.JSInterop;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Announces messages to hidden live regions in <c>MainLayout</c> for screen readers (Phase 13).
/// </summary>
public sealed class ScreenReaderAnnouncer(IJSRuntime jsRuntime, ILogger<ScreenReaderAnnouncer> logger)
{
    private IJSObjectReference? _module;

    /// <summary>
    /// Posts text to the polite or assertive announcer region.
    /// </summary>
    /// <param name="message">Human-readable message.</param>
    /// <param name="priority"><c>polite</c> or <c>assertive</c>.</param>
    public async Task AnnounceAsync(string message, string priority = "polite")
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/announcer.js");
            await _module.InvokeVoidAsync("announce", message, priority);
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone — not actionable.
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Screen reader announcement skipped.");
        }
    }
}
