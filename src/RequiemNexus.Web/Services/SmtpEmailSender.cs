using System.Net;
using System.Net.Mail;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

public partial class SmtpEmailSender : IRequiemEmailService
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(ILogger<SmtpEmailSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        await SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        await SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        await SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");
    }

    public Task SendEmailChangeLinkAsync(ApplicationUser user, string newEmail, string changeLink) =>
        SendEmailAsync(newEmail, "Confirm your new email address", $"Please confirm your new email address by <a href='{changeLink}'>clicking here</a>. If you did not request this change, you can ignore this email.");

    public Task SendAccountRecoveryCodeAsync(ApplicationUser user, string email, string code) =>
        SendEmailAsync(email, "Requiem Nexus Account Recovery", $"Your account recovery code is: <strong>{code}</strong>. This code expires in 15 minutes. Use it to disable 2FA and regain access to your account.");

    private async Task SendEmailAsync(string to, string subject, string htmlMessage)
    {
        try
        {
            var host = _configuration["Smtp:Host"];
            var portString = _configuration["Smtp:Port"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var from = _configuration["Smtp:From"] ?? "noreply@requiemnexus.com";

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString) || !int.TryParse(portString, out int port))
            {
                LogSmtpConfigMissing();
                LogDevEmailSenderFallback(to, subject, htmlMessage);
                return;
            }

            if (username == "SetInUserSecrets" || password == "SetInUserSecrets")
            {
                LogSmtpCredentialsMissing();
                LogDevEmailSenderFallback(to, subject, htmlMessage);
                return;
            }

            using var client = new SmtpClient(host, port);
            client.EnableSsl = true;
            if (!string.IsNullOrEmpty(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(from),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
            LogEmailSent(to, subject);
        }
        catch (Exception ex)
        {
            LogEmailSendError(ex, to);
            LogDevEmailSenderFallback(to, subject, htmlMessage);

            // We don't throw here to avoid disrupting the user registration flow in case of SMTP misconfiguration
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SMTP configuration is missing or invalid. Falling back to log-only email sender.")]
    partial void LogSmtpConfigMissing();

    [LoggerMessage(Level = LogLevel.Warning, Message = "SMTP credentials are set to placeholder values ('SetInUserSecrets'). Falling back to log-only email sender.")]
    partial void LogSmtpCredentialsMissing();

    [LoggerMessage(Level = LogLevel.Information, Message = "--- DEV EMAIL SENDER ---\nTo: {Email}\nSubject: {Subject}\nBody: {Body}\n------------------------")]
    partial void LogDevEmailSenderFallback(string email, string subject, string body);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email successfully sent to {Email} with subject {Subject}")]
    partial void LogEmailSent(string email, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while sending email to {Email}")]
    partial void LogEmailSendError(Exception ex, string email);
}
