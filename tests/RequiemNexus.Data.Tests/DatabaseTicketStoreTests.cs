using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Services;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class DatabaseTicketStoreTests
{
    private readonly DbContextOptions<ApplicationDbContext> _options;

    public DatabaseTicketStoreTests()
    {
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private AuthenticationTicket CreateMockTicket(string userId, DateTimeOffset? expiresAt = null)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            ExpiresUtc = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30)
        };
        return new AuthenticationTicket(principal, properties, "TestScheme");
    }

    public class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    [Fact]
    public async Task StoreAsync_ValidTicket_SavesToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var ticket = CreateMockTicket(userId);

        var mockHttpContextAccessor = new FakeHttpContextAccessor();
        var context = new DefaultHttpContext();
        context.Request.Headers.UserAgent = "TestAgent";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        mockHttpContextAccessor.HttpContext = context;

        using var dbContext = new ApplicationDbContext(_options);
        var store = new DatabaseTicketStore(dbContext, new NullLogger<DatabaseTicketStore>(), mockHttpContextAccessor);

        // Act
        var resultId = await store.StoreAsync(ticket);

        // Assert
        Assert.False(string.IsNullOrEmpty(resultId));
        var savedSession = await dbContext.UserSessions.FindAsync(resultId);
        Assert.NotNull(savedSession);
        Assert.Equal(userId, savedSession.ApplicationUserId);
        Assert.Equal("TestAgent", savedSession.UserAgent);
        Assert.Equal("127.0.0.1", savedSession.IpAddress);
        Assert.NotNull(savedSession.Value);
    }

    [Fact]
    public async Task RetrieveAsync_ExistingSession_ReturnsTicket()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var ticket = CreateMockTicket(userId);
        var mockHttpContextAccessor = new FakeHttpContextAccessor();

        string sessionId = string.Empty;
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var store = new DatabaseTicketStore(dbContext, new NullLogger<DatabaseTicketStore>(), mockHttpContextAccessor);
            sessionId = await store.StoreAsync(ticket);
        }

        // Act
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var store = new DatabaseTicketStore(dbContext, new NullLogger<DatabaseTicketStore>(), mockHttpContextAccessor);
            var retrievedTicket = await store.RetrieveAsync(sessionId);

            // Assert
            Assert.NotNull(retrievedTicket);
            Assert.Equal(userId, retrievedTicket.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }

    [Fact]
    public async Task RenewAsync_UpdatesExistingSession()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var originalTicket = CreateMockTicket(userId);
        var mockHttpContextAccessor = new FakeHttpContextAccessor();

        string sessionId = string.Empty;
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var store = new DatabaseTicketStore(dbContext, new NullLogger<DatabaseTicketStore>(), mockHttpContextAccessor);
            sessionId = await store.StoreAsync(originalTicket);
        }

        // Provide a new context for renewal, e.g. new IP or UserAgent, and new explicit expiration
        var renewContext = new DefaultHttpContext();
        renewContext.Request.Headers.UserAgent = "NewAgent";
        renewContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        mockHttpContextAccessor.HttpContext = renewContext;

        // Trim fractional seconds to avoid SQLite/InMemory truncation mismatch
        var now = DateTimeOffset.UtcNow.AddDays(1);
        var newExpiration = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Offset);
        var newTicket = CreateMockTicket(userId, newExpiration);

        // Act
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var store = new DatabaseTicketStore(dbContext, new NullLogger<DatabaseTicketStore>(), mockHttpContextAccessor);
            await store.RenewAsync(sessionId, newTicket);
        }

        // Assert
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var updatedSession = await dbContext.UserSessions.FindAsync(sessionId);
            Assert.NotNull(updatedSession);
            Assert.Equal("NewAgent", updatedSession.UserAgent);
            Assert.Equal("192.168.1.1", updatedSession.IpAddress);
            Assert.Equal(newExpiration, updatedSession.ExpiresAt);
        }
    }

    [Fact]
    public async Task RemoveAsync_DeletesSession()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var ticket = CreateMockTicket(userId);
        var mockHttpContextAccessor = new FakeHttpContextAccessor();

        string sessionId = string.Empty;
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var store = new DatabaseTicketStore(dbContext, new NullLogger<DatabaseTicketStore>(), mockHttpContextAccessor);
            sessionId = await store.StoreAsync(ticket);
        }

        // Act
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var store = new DatabaseTicketStore(dbContext, new NullLogger<DatabaseTicketStore>(), mockHttpContextAccessor);
            await store.RemoveAsync(sessionId);
        }

        // Assert
        using (var dbContext = new ApplicationDbContext(_options))
        {
            var deletedSession = await dbContext.UserSessions.FindAsync(sessionId);
            Assert.Null(deletedSession);
        }
    }
}
