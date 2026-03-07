using Microsoft.AspNetCore.Identity;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

public partial class DevEmailSender : IEmailSender<ApplicationUser>
{
    private readonly ILogger<DevEmailSender> _logger;

    public DevEmailSender(ILogger<DevEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        LogConfirmationLink(_logger, email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        LogPasswordResetCode(_logger, email, resetCode);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        LogPasswordResetLink(_logger, email, resetLink);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = """
        --- DEV EMAIL SENDER ---
        To: {Email}
        Subject: Confirm your email
        Link: {ConfirmationLink}
        ------------------------
        """)]
    static partial void LogConfirmationLink(ILogger logger, string email, string confirmationLink);

    [LoggerMessage(Level = LogLevel.Information, Message = """
        --- DEV EMAIL SENDER ---
        To: {Email}
        Subject: Reset your password
        Code: {ResetCode}
        ------------------------
        """)]
    static partial void LogPasswordResetCode(ILogger logger, string email, string resetCode);

    [LoggerMessage(Level = LogLevel.Information, Message = """
        --- DEV EMAIL SENDER ---
        To: {Email}
        Subject: Reset your password
        Link: {ResetLink}
        ------------------------
        """)]
    static partial void LogPasswordResetLink(ILogger logger, string email, string resetLink);
}
