using Microsoft.Playwright;
using RequiemNexus.E2E.Tests.Fixtures;
using Xunit;

namespace RequiemNexus.E2E.Tests.Tests.Smoke;

/// <summary>
/// Verifies the in-process host and Playwright wiring (shared infrastructure).
/// </summary>
[Collection("E2e")]
public sealed class HostReachabilityTests
{
    private readonly AppFixture _app;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostReachabilityTests"/> class.
    /// </summary>
    /// <param name="app">Shared app fixture.</param>
    public HostReachabilityTests(AppFixture app) => _app = app;

    /// <summary>
    /// The login experience is routed at <c>/</c>; ensure the Blazor page renders the expected heading.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task LoginRoute_LoadsWelcomeHeading()
    {
        await using IBrowserContext context = await _app.Browser.NewContextAsync(_app.NewContextOptions());
        context.SetDefaultTimeout(5_000);
        context.SetDefaultNavigationTimeout(15_000);
        IPage page = await context.NewPageAsync();
        await page.GotoAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        ILocator heading = page.GetByRole(AriaRole.Heading, new() { Name = "Welcome Back" });
        await heading.WaitForAsync();
        Assert.True(await heading.IsVisibleAsync());
    }
}
