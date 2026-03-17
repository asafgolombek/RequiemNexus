using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Background service that permanently deletes accounts whose 30-day grace period has expired.
/// Runs once daily. GDPR-compliant: data is erased when the user's deletion date is reached.
/// Cascade order: (1) delete all campaigns the user storytells (null-outs enrolled characters),
/// (2) delete all remaining user characters, (3) delete the identity user record.
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
        using IServiceScope scope = scopeFactory.CreateScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ICampaignService campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();
        ICharacterService characterService = scope.ServiceProvider.GetRequiredService<ICharacterService>();

        DateTimeOffset now = DateTimeOffset.UtcNow;

        List<ApplicationUser> dueForDeletion = await dbContext.Users
            .Where(u => u.DeletionScheduledAt != null && u.DeletionScheduledAt <= now)
            .ToListAsync();

        if (dueForDeletion.Count == 0)
        {
            return;
        }

        logger.LogInformation("Purging {Count} account(s) past their deletion grace period.", dueForDeletion.Count);

        foreach (ApplicationUser user in dueForDeletion)
        {
            try
            {
                // Step 1: Delete all campaigns the user storytells.
                // This nulls out CampaignId on all enrolled characters before removing the campaign row.
                List<Campaign> storytoldCampaigns = await campaignService.GetStorytoldCampaignsAsync(user.Id);
                foreach (Campaign campaign in storytoldCampaigns)
                {
                    await campaignService.DeleteCampaignAsync(campaign.Id, user.Id);
                }

                // Step 2: Delete all remaining characters owned by the user.
                List<Character> characters = await characterService.GetCharactersByUserIdAsync(user.Id);
                foreach (Character character in characters)
                {
                    await characterService.DeleteCharacterAsync(character.Id, user.Id);
                }

                // Step 3: Delete the identity record.
                IdentityResult result = await userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    logger.LogInformation("Permanently deleted account for user {UserId}.", user.Id);
                }
                else
                {
                    string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    logger.LogError("Failed to delete account for user {UserId}: {Errors}", user.Id, errors);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error purging account for user {UserId}.", user.Id);
            }
        }
    }
}
