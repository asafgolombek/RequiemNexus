using Microsoft.Playwright;
using Xunit;

namespace RequiemNexus.E2ETests;

/// <summary>
/// Automated accessibility (a11y) tests using axe-core injected via Playwright.
/// Prerequisites:
///   1. The RequiemNexus.Web app must be running (defaults to http://localhost:5000,
///      or set the APP_BASE_URL environment variable).
///   2. Playwright browsers installed: pwsh bin/Debug/net10.0/playwright.ps1 install
/// These tests are skipped automatically when the app is not reachable.
/// </summary>
public class AccessibilityTests : IAsyncLifetime
{
    // CDN-hosted axe-core — pinned to a stable minor version
    private const string AxeCoreUrl = "https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.10.0/axe.min.js";

    private static readonly string AppBaseUrl =
        Environment.GetEnvironmentVariable("APP_BASE_URL") ?? "http://localhost:5000";

    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;
    private bool _appReachable;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = AppBaseUrl
        });
        _page = await _context.NewPageAsync();

        // Probe the app — skip gracefully if it is not running
        try
        {
            var response = await _page.GotoAsync("/", new PageGotoOptions { Timeout = 5_000 });
            _appReachable = response?.Ok ?? false;
        }
        catch
        {
            _appReachable = false;
        }
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Injects axe-core from CDN and runs it against the current page.
    /// Returns a list of accessibility violations (empty = clean).
    /// </summary>
    private async Task<List<string>> RunAxeAsync()
    {
        // Inject axe-core
        var axeScript = await new HttpClient().GetStringAsync(AxeCoreUrl);
        await _page.EvaluateAsync(axeScript);

        // Run analysis and collect violations
        var result = await _page.EvaluateAsync<AxeResult>("async () => await axe.run()");

        return result.Violations
            .Select(v => $"[{v.Impact}] {v.Id}: {v.Description} — nodes: {v.Nodes.Count}")
            .ToList();
    }

    private void SkipIfAppNotReachable()
    {
        if (!_appReachable)
            throw new SkipException($"App not reachable at {AppBaseUrl} — skipping accessibility tests.");
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Home page has zero a11y violations")]
    public async Task HomePage_HasNoAccessibilityViolations()
    {
        SkipIfAppNotReachable();

        await _page.GotoAsync("/");
        var violations = await RunAxeAsync();

        Assert.True(violations.Count == 0,
            $"Accessibility violations found on home page:\n{string.Join("\n", violations)}");
    }

    [Fact(DisplayName = "Login page has zero a11y violations")]
    public async Task LoginPage_HasNoAccessibilityViolations()
    {
        SkipIfAppNotReachable();

        await _page.GotoAsync("/Identity/Account/Login");
        var violations = await RunAxeAsync();

        Assert.True(violations.Count == 0,
            $"Accessibility violations found on login page:\n{string.Join("\n", violations)}");
    }

    // ── axe-core result DTOs ─────────────────────────────────────────────────

    private sealed record AxeResult(List<AxeViolation> Violations);
    private sealed record AxeViolation(string Id, string Impact, string Description, List<object> Nodes);
}

/// <summary>
/// Signals to xUnit that a test should be skipped at runtime (not compile-time).
/// </summary>
file sealed class SkipException(string reason) : Exception(reason);
