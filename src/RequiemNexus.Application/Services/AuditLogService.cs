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
        var auditLogs = await dbContext.AuditLogs
            .Where(l => l.UserId == userId)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();
        return auditLogs.OrderByDescending(l => l.OccurredAt).ToList();
    }
}
