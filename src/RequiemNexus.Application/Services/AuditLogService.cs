using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

public class AuditLogService(ApplicationDbContext dbContext) : IAuditLogService
{
    public async Task LogAsync(string userId, AuditEventType eventType, string? ipAddress = null, string? details = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            EventType = eventType,
            OccurredAt = DateTimeOffset.UtcNow,
            IpAddress = ipAddress,
            Details = details,
        };

        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentEventsAsync(string userId, int limit = 50)
    {
        // AuditLog.OccurredAt is DateTimeOffset, stored as TEXT by SQLite.
        // EF Core cannot reliably translate OrderByDescending on DateTimeOffset to SQLite SQL,
        // so we sort in memory after loading. Take(limit) must come AFTER sorting to avoid
        // silently returning the N most-recently-inserted rows instead of the N most-recent by date.
        var auditLogs = await dbContext.AuditLogs
            .Where(l => l.UserId == userId)
            .AsNoTracking()
            .ToListAsync();

        return [.. auditLogs.OrderByDescending(l => l.OccurredAt).Take(limit)];
    }
}
