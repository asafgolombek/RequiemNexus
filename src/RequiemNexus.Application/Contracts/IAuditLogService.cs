using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface IAuditLogService
{
    Task LogAsync(string userId, AuditEventType eventType, string? ipAddress = null, string? details = null);

    Task<List<AuditLog>> GetRecentEventsAsync(string userId, int limit = 50);
}
