using Microsoft.Playwright;
using Xunit;

namespace RequiemNexus.VisualRegression.Tests.Tests;

/// <summary>
/// Smoke guard: login page uses a non-empty computed background (catches missing theme / CSS).
/// </summary>
[Trait("Category", "VisualRegression")]
[Collection("Vr")]
public sealed class PaletteAuditTests
{
    private readonly SnapshotFixture _fx;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaletteAuditTests"/> class.
    /// </summary>
    public PaletteAuditTests(SnapshotFixture fx) => _fx = fx;

    /// <summary>
    /// Ensures the themed background is applied on the login shell.
    /// </summary>
    [Fact]
    public async Task LoginPage_HasNonEmptyBodyBackground()
    {
        await using IBrowserContext context = await _fx.Browser.NewContextAsync(_fx.NewContextOptions());
        IPage page = await context.NewPageAsync();
        await page.GotoAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        string? bg = await page.EvaluateAsync<string>(
            "() => getComputedStyle(document.body).backgroundColor");
        Assert.False(string.IsNullOrWhiteSpace(bg));
        Assert.NotEqual("rgba(0, 0, 0, 0)", bg);
    }
}
