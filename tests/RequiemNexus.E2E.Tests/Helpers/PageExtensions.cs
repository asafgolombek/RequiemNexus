using Microsoft.Playwright;

namespace RequiemNexus.E2E.Tests.Helpers;

/// <summary>
/// Shared Playwright helpers for Blazor Server flows (Track 1).
/// </summary>
public static class PageExtensions
{
    /// <summary>
    /// Waits for a toast item in <c>ToastContainer</c> (polite/assertive region).
    /// </summary>
    /// <param name="page">Active page.</param>
    /// <param name="timeoutMs">Maximum wait in milliseconds.</param>
    public static Task WaitForToastAsync(this IPage page, int timeoutMs = 15_000) =>
        page.Locator(".toast-item").First.WaitForAsync(new LocatorWaitForOptions { Timeout = timeoutMs });
}
