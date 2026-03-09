using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;

namespace RequiemNexus.Application.Services;

public class UserDataExportService(ApplicationDbContext dbContext) : IUserDataExportService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public async Task<string> ExportUserDataAsJsonAsync(string userId)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return "{}";
        }

        var characters = await dbContext.Characters
            .Include(c => c.Clan)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
            .Include(c => c.Aspirations)
            .Include(c => c.Banes)
            .Where(c => c.ApplicationUserId == userId)
            .AsNoTracking()
            .ToListAsync();

        var auditLogs = await dbContext.AuditLogs
            .Where(l => l.UserId == userId)
            .AsNoTracking()
            .ToListAsync();

        auditLogs = auditLogs.OrderByDescending(l => l.OccurredAt).ToList();

        var export = new
        {
            exportedAt = DateTimeOffset.UtcNow,
            profile = new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.Birthday,
                user.MemberSince,
                user.TwoFactorEnabled,
                user.EmailConfirmed,
            },
            characters = characters.Select(c => new
            {
                c.Id,
                c.Name,
                c.Concept,
                c.Mask,
                c.Dirge,
                c.Touchstone,
                c.Backstory,
                c.Height,
                c.EyeColor,
                c.HairColor,
                clan = c.Clan?.Name,
                c.BloodPotency,
                c.Humanity,
                c.MaxHealth,
                c.CurrentHealth,
                c.MaxWillpower,
                c.CurrentWillpower,
                c.MaxVitae,
                c.CurrentVitae,
                c.ExperiencePoints,
                c.TotalExperiencePoints,
                c.Beats,
                c.Size,
                c.Speed,
                c.Defense,
                c.Armor,
                attributes = c.Attributes.Select(a => new { a.Name, a.Rating }),
                skills = c.Skills.Select(s => new { s.Name, s.Rating }),
                merits = c.Merits.Select(m => new { m.Merit?.Name, m.Specification, m.Rating }),
                disciplines = c.Disciplines.Select(d => new { d.Discipline?.Name, d.Rating }),
                aspirations = c.Aspirations.Select(a => new { a.Description }),
                banes = c.Banes.Select(b => new { b.Description }),
            }),
            securityEvents = auditLogs.Select(l => new
            {
                eventType = l.EventType.ToString(),
                l.OccurredAt,
                l.IpAddress,
                l.Details,
            }),
        };

        return JsonSerializer.Serialize(export, _jsonOptions);
    }
}
