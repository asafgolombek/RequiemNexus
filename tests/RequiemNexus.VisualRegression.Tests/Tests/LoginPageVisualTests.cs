using Microsoft.Playwright;
using Xunit;

namespace RequiemNexus.VisualRegression.Tests.Tests;

/// <summary>
/// Login route visual smoke: full-page PNG is captured (Playwright .NET does not yet expose
/// <c>Expect(Page).ToHaveScreenshotAsync</c>; pixel baselines can be added when available).
/// </summary>
[Trait("Category", "VisualRegression")]
[Collection("Vr")]
public sealed class LoginPageVisualTests
{
    private readonly SnapshotFixture _fx;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginPageVisualTests"/> class.
    /// </summary>
    public LoginPageVisualTests(SnapshotFixture fx) => _fx = fx;

    /// <summary>
    /// Captures a full-page PNG and optionally writes <c>Snapshots/login-chromium.png</c> when
    /// <c>PLAYWRIGHT_UPDATE_SNAPSHOTS=1</c>.
    /// </summary>
    [Fact]
    public async Task LoginRoute_FullPagePng_IsCaptured()
    {
        await using IBrowserContext context = await _fx.Browser.NewContextAsync(_fx.NewContextOptions());
        context.SetDefaultNavigationTimeout(30_000);
        IPage page = await context.NewPageAsync();
        await page.GotoAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        byte[] png = await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true, Type = ScreenshotType.Png });

        Assert.True(png.Length > 8_000, "Expected a non-trivial PNG payload.");
        Assert.Equal(0x89, png[0]);
        Assert.Equal(0x50, png[1]);
        Assert.Equal(0x4E, png[2]);
        Assert.Equal(0x47, png[3]);

        bool update = string.Equals(
            Environment.GetEnvironmentVariable("PLAYWRIGHT_UPDATE_SNAPSHOTS"),
            "1",
            StringComparison.OrdinalIgnoreCase);
        if (update)
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Snapshots");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, "login-chromium.png");
            await File.WriteAllBytesAsync(path, png);
        }
    }
}
