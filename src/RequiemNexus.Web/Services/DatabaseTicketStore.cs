using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
namespace RequiemNexus.Web.Services;

public class DatabaseTicketStore(IServiceScopeFactory scopeFactory, ILogger<DatabaseTicketStore> logger, IHttpContextAccessor httpContextAccessor) : ITicketStore
{
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var userId = ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return string.Empty;
        }

        var id = Guid.NewGuid().ToString();
        var ticketData = SerializeToBytes(ticket);

        var httpContext = httpContextAccessor.HttpContext;
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();

        var session = new UserSession
        {
            Id = id,
            ApplicationUserId = userId,
            Value = ticketData,
            LastActive = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = ticket.Properties.ExpiresUtc,
            UserAgent = userAgent,
            IpAddress = ipAddress
        };

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.UserSessions.Add(session);
        await dbContext.SaveChangesAsync();

        return id;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await dbContext.UserSessions.FindAsync(key);
        if (session != null)
        {
            session.Value = SerializeToBytes(ticket);
            session.LastActive = DateTimeOffset.UtcNow;
            session.ExpiresAt = ticket.Properties.ExpiresUtc;

            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                session.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
                session.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            }

            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await dbContext.UserSessions.FindAsync(key);
        if (session == null)
            return null;

        // Optionally, update LastActive here as well. Note: reading a session happens often, 
        // updating the DB on every read might impact performance. The RenewAsync method 
        // handles updating sliding expirations, which is generally sufficient.

        return DeserializeFromBytes(session.Value);
    }

    public async Task RemoveAsync(string key)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var session = await dbContext.UserSessions.FindAsync(key);
        if (session != null)
        {
            logger.LogInformation("Removing session ticket with ID {SessionId}", key);
            dbContext.UserSessions.Remove(session);
            await dbContext.SaveChangesAsync();
        }
    }

    private static byte[] SerializeToBytes(AuthenticationTicket ticket)
    {
        // Using TicketSerializer to securely serialize the ticket
        return TicketSerializer.Default.Serialize(ticket);
    }

    private static AuthenticationTicket? DeserializeFromBytes(byte[] source)
    {
        return TicketSerializer.Default.Deserialize(source);
    }
}
