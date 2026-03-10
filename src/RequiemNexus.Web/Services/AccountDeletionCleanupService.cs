using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Background service that permanently deletes accounts whose 30-day grace period has expired.
/// Runs once daily. GDPR-compliant: data is erased when the user's deletion date is reached.
/// </summary>
public class AccountDeletionCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<AccountDeletionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay first run until the host has fully started to avoid racing with migrations.
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PurgeExpiredAccountsAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Log and continue — a transient error must never crash the host.
                logger.LogError(ex, "Error during account deletion cleanup.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task PurgeExpiredAccountsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateTimeOffset.UtcNow;

        var dueForDeletion = await dbContext.Users
            .Where(u => u.DeletionScheduledAt != null && u.DeletionScheduledAt <= now)
            .ToListAsync();

        if (dueForDeletion.Count == 0)
        {
            return;
        }

        logger.LogInformation("Purging {Count} account(s) past their deletion grace period.", dueForDeletion.Count);

        foreach (var user in dueForDeletion)
        {
            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                logger.LogInformation("Permanently deleted account for user {UserId}.", user.Id);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to delete account for user {UserId}: {Errors}", user.Id, errors);
            }
        }
    }
}
