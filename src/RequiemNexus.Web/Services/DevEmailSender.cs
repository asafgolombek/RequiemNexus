using Microsoft.AspNetCore.Identity;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

public class DevEmailSender(ILogger<DevEmailSender> logger) : IEmailSender<ApplicationUser>
{
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        logger.LogInformation("""
            --- DEV EMAIL SENDER ---
            To: {Email}
            Subject: Confirm your email
            Link: {ConfirmationLink}
            ------------------------
            """, email, confirmationLink);
        return Task.CompletedTask;
    }


    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        logger.LogInformation("""
            --- DEV EMAIL SENDER ---
            To: {Email}
            Subject: Reset your password
            Code: {ResetCode}
            ------------------------
            """, email, resetCode);
        return Task.CompletedTask;
    }


    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        logger.LogInformation("""
            --- DEV EMAIL SENDER ---
            To: {Email}
            Subject: Reset your password
            Link: {ResetLink}
            ------------------------
            """, email, resetLink);
        return Task.CompletedTask;
    }

}
