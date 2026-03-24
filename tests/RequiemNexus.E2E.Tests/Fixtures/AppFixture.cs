using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Moq;
using RequiemNexus.Data;
using RequiemNexus.Web;
using StackExchange.Redis;
using Xunit;

namespace RequiemNexus.E2E.Tests.Fixtures;

/// <summary>
/// In-process host (<see cref="WebApplicationFactory{TEntryPoint}"/>) with <c>Testing</c> environment,
/// PostgreSQL migrations + seed, Playwright browser, and a non-connecting Redis multiplexer substitute.
/// </summary>
public class AppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppFixture"/> class.
    /// </summary>
    public AppFixture() => UseKestrel();

    /// <summary>
    /// Gets the shared Chromium instance for this fixture instance (one per test class).
    /// </summary>
    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser not initialized.");

    /// <summary>
    /// Seeded campaign id (ST is the E2E user).
    /// </summary>
    public int SeededCampaignId { get; private set; }

    /// <summary>
    /// Seeded player character id.
    /// </summary>
    public int SeededCharacterId { get; private set; }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            foreach (ServiceDescriptor d in services.Where(d => d.ServiceType == typeof(IConnectionMultiplexer)).ToList())
            {
                services.Remove(d);
            }

            var mockRedis = new Mock<IConnectionMultiplexer>();
            var mockDb = new Mock<IDatabase>();
            var mockBatch = new Mock<IBatch>();
            var mockSubscriber = new Mock<ISubscriber>(MockBehavior.Loose);

            mockRedis.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
            mockRedis.Setup(m => m.GetSubscriber(It.IsAny<object>())).Returns(mockSubscriber.Object);
            mockDb.Setup(d => d.CreateBatch(It.IsAny<object>())).Returns(mockBatch.Object);

            services.AddSingleton(mockRedis.Object);
        });
    }

    /// <summary>
    /// Creates Playwright context options for the running test server (viewport matches Phase 13 defaults).
    /// </summary>
    /// <returns>Context options with <see cref="BrowserNewContextOptions.BaseURL"/> set.</returns>
    public BrowserNewContextOptions NewContextOptions()
    {
        string baseUrl = ResolvePublicBaseUrl();
        if (!baseUrl.EndsWith('/'))
        {
            baseUrl += "/";
        }

        return new BrowserNewContextOptions
        {
            BaseURL = baseUrl,
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
        };
    }

    /// <summary>
    /// Public base URL for Chromium after Kestrel has bound (see <see cref="WebApplicationFactory{TEntryPoint}.ClientOptions"/>).
    /// </summary>
    private string ResolvePublicBaseUrl()
    {
        Uri? clientBase = ClientOptions.BaseAddress
            ?? throw new InvalidOperationException("Kestrel base address is not available yet.");
        return clientBase.GetLeftPart(UriPartial.Authority);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await DbInitializer.InitializeAsync(db, roleManager, runMigrations: false);

        (SeededCampaignId, SeededCharacterId) =
            await E2eTestDataSeed.EnsurePlayerCampaignAndCharacterAsync(Services);
    }

    /// <inheritdoc />
    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        Dispose();
    }
}
