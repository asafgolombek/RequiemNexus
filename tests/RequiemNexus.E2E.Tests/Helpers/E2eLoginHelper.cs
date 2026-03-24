using Microsoft.Playwright;
using RequiemNexus.E2E.Tests.Fixtures;

namespace RequiemNexus.E2E.Tests.Helpers;

/// <summary>
/// Performs cookie login against the in-process host using seeded credentials.
/// </summary>
public static class E2eLoginHelper
{
    /// <summary>
    /// Signs in via the root login form and waits for the home dashboard.
    /// </summary>
    /// <param name="page">Playwright page (same origin as <see cref="AppFixture"/>).</param>
    public static async Task LoginAsSeededPlayerAsync(IPage page)
    {
        await page.GotoAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.Locator("#email").FillAsync(E2eTestDataSeed.PlayerEmail);
        await page.Locator("#password").FillAsync(E2eTestDataSeed.Password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Sign In" }).ClickAsync();
        await page.WaitForURLAsync("**/home**", new() { Timeout = 30_000 });
    }
}
