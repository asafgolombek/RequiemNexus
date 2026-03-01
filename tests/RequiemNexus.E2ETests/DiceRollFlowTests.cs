using Microsoft.Playwright;
using Xunit;

namespace RequiemNexus.E2ETests;

/// <summary>
/// End-to-end tests for the Dice Nexus roll flow.
/// Prerequisites:
///   1. The RequiemNexus.Web app must be running on http://localhost:5000
///   2. At least one character must exist for the test user (seed data or prior creation test)
/// </summary>
public class DiceRollFlowTests : IAsyncLifetime
{
    private const string AppBaseUrl = "http://localhost:5000";
    private const string TestUserEmail = "e2etest@requiemnexus.local";
    private const string TestUserPassword = "test";

    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

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

        await LoginAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    private async Task LoginAsync()
    {
        await _page.GotoAsync("/Account/Login");
        await _page.FillAsync("input[name='Input.Email']", TestUserEmail);
        await _page.FillAsync("input[name='Input.Password']", TestUserPassword);
        await _page.ClickAsync("button[type='submit']");
        await _page.WaitForURLAsync("**/");
    }

    private async Task<IPage> NavigateToFirstCharacterSheetAsync()
    {
        await _page.GotoAsync("/");

        // Click the first character card to navigate to its sheet
        var firstCharacterLink = _page.Locator("a[id^='character-card-']").First;
        await firstCharacterLink.ClickAsync();
        await _page.WaitForURLAsync("**/character/**", new PageWaitForURLOptions { Timeout = 10_000 });

        return _page;
    }

    [Fact]
    public async Task DiceNexus_RollButton_IsVisibleOnCharacterSheet()
    {
        await NavigateToFirstCharacterSheetAsync();

        var rollButton = _page.Locator("button[id='roll-dice-button']");
        await rollButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5_000 });

        Assert.True(await rollButton.IsVisibleAsync());
    }

    [Fact]
    public async Task DiceNexus_PerformRoll_DisplaysResult()
    {
        await NavigateToFirstCharacterSheetAsync();

        // Open the dice panel if collapsed
        var diceSection = _page.Locator("[id='dice-nexus-panel']");
        if (await diceSection.IsHiddenAsync())
            await _page.ClickAsync("[id='dice-nexus-toggle']");

        // Click roll
        await _page.ClickAsync("button[id='roll-dice-button']");

        // The roll result area should appear
        var resultArea = _page.Locator("[id='roll-result']");
        await resultArea.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5_000 });

        var resultText = await resultArea.TextContentAsync();
        Assert.False(string.IsNullOrWhiteSpace(resultText));
    }
}
