using Microsoft.AspNetCore.Identity;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

public class DevEmailSender(ILogger<DevEmailSender> logger) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        logger.LogInformation("--- DEV EMAIL SENDER ---");
        logger.LogInformation("To: {Email}", email);
        logger.LogInformation("Subject: Confirm your email");
        logger.LogInformation("Link: {ConfirmationLink}", confirmationLink);
        logger.LogInformation("------------------------");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        logger.LogInformation("--- DEV EMAIL SENDER ---");
        logger.LogInformation("To: {Email}", email);
        logger.LogInformation("Subject: Reset your password");
        logger.LogInformation("Code: {ResetCode}", resetCode);
        logger.LogInformation("------------------------");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        logger.LogInformation("--- DEV EMAIL SENDER ---");
        logger.LogInformation("To: {Email}", email);
        logger.LogInformation("Subject: Reset your password");
        logger.LogInformation("Link: {ResetLink}", resetLink);
        logger.LogInformation("------------------------");
        return Task.CompletedTask;
    }
}
