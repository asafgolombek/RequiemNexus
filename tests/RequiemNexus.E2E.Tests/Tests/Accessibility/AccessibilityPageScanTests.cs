using Microsoft.Playwright;
using RequiemNexus.E2E.Tests.Fixtures;
using RequiemNexus.E2E.Tests.Helpers;
using Xunit;

namespace RequiemNexus.E2E.Tests.Tests.Accessibility;

/// <summary>
/// WCAG 2.1 AA axe scans for Phase 13 Track 2 (filtered locally via <c>Category=Accessibility</c>).
/// </summary>
[Trait("Category", "Accessibility")]
[Collection("E2e")]
public sealed class AccessibilityPageScanTests
{
    private readonly AppFixture _app;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessibilityPageScanTests"/> class.
    /// </summary>
    /// <param name="app">Shared host fixture.</param>
    public AccessibilityPageScanTests(AppFixture app) => _app = app;

    private async Task WithPageAsync(Func<IPage, Task> body)
    {
        await using IBrowserContext context = await _app.Browser.NewContextAsync(_app.NewContextOptions());
        context.SetDefaultTimeout(15_000);
        context.SetDefaultNavigationTimeout(30_000);
        IPage page = await context.NewPageAsync();
        await body(page);
    }

    /// <summary>Login route (root).</summary>
    [Fact]
    public async Task LoginRoute_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await page.GotoAsync("/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Registration.</summary>
    [Fact]
    public async Task RegisterRoute_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await page.GotoAsync("/register");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Post-login home dashboard.</summary>
    [Fact]
    public async Task HomeDashboard_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await E2eLoginHelper.LoginAsSeededPlayerAsync(page);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Character roster.</summary>
    [Fact]
    public async Task CharacterRoster_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await E2eLoginHelper.LoginAsSeededPlayerAsync(page);
            await page.GotoAsync("/characters");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Character sheet (details).</summary>
    [Fact]
    public async Task CharacterSheet_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await E2eLoginHelper.LoginAsSeededPlayerAsync(page);
            await page.GotoAsync($"/character/{_app.SeededCharacterId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Advancement / edit-style flows.</summary>
    [Fact]
    public async Task CharacterAdvancement_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await E2eLoginHelper.LoginAsSeededPlayerAsync(page);
            await page.GotoAsync($"/character/{_app.SeededCharacterId}/advancement");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Pack tab on the character sheet.</summary>
    [Fact]
    public async Task CharacterPackTab_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await E2eLoginHelper.LoginAsSeededPlayerAsync(page);
            await page.GotoAsync($"/character/{_app.SeededCharacterId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.GetByRole(AriaRole.Tab, new() { Name = "The Pack" }).ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Blood Sorcery heading region on the character sheet (overview tab).</summary>
    [Fact]
    public async Task CharacterBloodSorcerySection_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await E2eLoginHelper.LoginAsSeededPlayerAsync(page);
            await page.GotoAsync($"/character/{_app.SeededCharacterId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            ILocator heading = page.GetByRole(AriaRole.Heading, new() { Name = "Blood Sorcery" });
            if (await heading.CountAsync() > 0)
            {
                await heading.First.ScrollIntoViewIfNeededAsync();
            }

            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Social maneuvers panel is rendered on the character sheet.</summary>
    [Fact]
    public async Task CharacterSocialManeuvers_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await E2eLoginHelper.LoginAsSeededPlayerAsync(page);
            await page.GotoAsync($"/character/{_app.SeededCharacterId}");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            ILocator section = page.Locator(".cs-social-maneuvers");
            if (await section.CountAsync() > 0)
            {
                await section.First.ScrollIntoViewIfNeededAsync();
            }

            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }

    /// <summary>Storyteller glimpse (seeded user is ST).</summary>
    [Fact]
    public async Task StorytellerGlimpse_HasNoCriticalAxeViolations()
    {
        await WithPageAsync(async page =>
        {
            await E2eLoginHelper.LoginAsSeededPlayerAsync(page);
            await page.GotoAsync($"/campaigns/{_app.SeededCampaignId}/glimpse");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await A11yHelper.AssertNoViolationsAsync(page);
        });
    }
}
