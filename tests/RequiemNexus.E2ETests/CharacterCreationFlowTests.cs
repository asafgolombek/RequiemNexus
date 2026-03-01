using Microsoft.Playwright;
using Xunit;
namespace RequiemNexus.E2ETests;

/// <summary>
/// End-to-end tests for the character creation flow.
/// Prerequisites:
///   1. The RequiemNexus.Web app must be running on http://localhost:5000
///   2. A test user must exist (see E2ETestFixture or seed data)
///   3. Playwright browsers installed: dotnet build + pwsh bin/Debug/net10.0/playwright.ps1 install
/// </summary>
public class CharacterCreationFlowTests : IAsyncLifetime
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

    [Fact]
    public async Task CreateCharacter_NavigatesToCreationPage()
    {
        await _page.GotoAsync("/create-character");

        var heading = await _page.TextContentAsync("h1");
        Assert.Contains("Embrace", heading ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCharacter_SubmitValidForm_RedirectsToCharacterSheet()
    {
        await _page.GotoAsync("/create-character");

        // Fill character name
        await _page.FillAsync("input[id='character-name']", "E2E Vampire");

        // Fill concept
        var conceptInput = _page.Locator("input[id='character-concept']");
        if (await conceptInput.IsVisibleAsync())
            await conceptInput.FillAsync("Wandering Neonate");

        // Click to submit (the "Embrace" button)
        await _page.ClickAsync("button[id='embrace-button']");

        // Should redirect to character sheet
        await _page.WaitForURLAsync("**/character/**", new PageWaitForURLOptions { Timeout = 10_000 });

        var url = _page.Url;
        Assert.Contains("/character/", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCharacter_CharacterNameAppearsInSheet()
    {
        await _page.GotoAsync("/create-character");

        var uniqueName = $"E2E-{Guid.NewGuid():N[..6]}";

        await _page.FillAsync("input[id='character-name']", uniqueName);
        await _page.ClickAsync("button[id='embrace-button']");
        await _page.WaitForURLAsync("**/character/**", new PageWaitForURLOptions { Timeout = 10_000 });

        var pageContent = await _page.ContentAsync();
        Assert.Contains(uniqueName, pageContent, StringComparison.OrdinalIgnoreCase);
    }
}
