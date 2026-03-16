using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using StackExchange.Redis;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim(ClaimTypes.Name, "test@test.com")
        };
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public class RealTimeTests : IClassFixture<WebApplicationFactory<RequiemNexus.Web.Components.App>>
{
    private readonly WebApplicationFactory<RequiemNexus.Web.Components.App> _factory;

    public RealTimeTests(WebApplicationFactory<RequiemNexus.Web.Components.App> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                // Replace real Redis with a mock
                var mockRedis = new Mock<IConnectionMultiplexer>();
                var mockDb = new Mock<IDatabase>();
                var mockBatch = new Mock<IBatch>();

                mockRedis.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
                mockRedis.Setup(m => m.GetSubscriber(It.IsAny<object>())).Returns(new Mock<ISubscriber>().Object);
                mockDb.Setup(d => d.CreateBatch(It.IsAny<object>())).Returns(mockBatch.Object);

                services.AddSingleton(mockRedis.Object);
                services.AddSingleton<ISessionStateRepository, SessionStateRepository>();
                services.AddSingleton(new Mock<IAuditLogService>().Object);

                // Replace DB with In-Memory
                var descriptors = services.Where(d =>
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true ||
                    d.ServiceType == typeof(ApplicationDbContext) ||
                    d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("TestDb"));
                services.AddDbContextFactory<ApplicationDbContext>(options => options.UseInMemoryDatabase("TestDb"));

                // Add test authentication
                services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
            });
        });
    }

    [Fact]
    public async Task ST_Can_Start_And_End_Session()
    {
        // Arrange
        // Seed a campaign where test-user is the ST
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            db.Campaigns.Add(new Campaign { Id = 1, Name = "Test", StoryTellerId = "test-user" });
            await db.SaveChangesAsync();
        }

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(_factory.Server.BaseAddress + "hubs/session", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        bool sessionStartedReceived = false;
        bool sessionEndedReceived = false;

        hubConnection.On("SessionStarted", () => sessionStartedReceived = true);
        hubConnection.On<string>("SessionEnded", _ => sessionEndedReceived = true);

        await hubConnection.StartAsync();

        // Join the group first
        await hubConnection.InvokeAsync("JoinSession", 1, (int?)null);

        // Act - Start Session
        await hubConnection.InvokeAsync("StartSession", 1);

        // Wait for broadcast
        await Task.Delay(500);

        // Assert
        Assert.True(sessionStartedReceived);

        // Act - End Session
        await hubConnection.InvokeAsync("EndSession", 1);
        await Task.Delay(500);

        // Assert
        Assert.True(sessionEndedReceived);

        await hubConnection.StopAsync();
    }
}
