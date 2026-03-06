using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Services;

#pragma warning disable SYSLIB0014 // Type or member is obsolete

public partial class SmtpEmailSender : IEmailSender<ApplicationUser>
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
                LogSmtpConfigMissing(_logger);
                LogDevEmailSenderFallback(_logger, to, subject, htmlMessage);
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
            LogEmailSent(_logger, to, subject);
        }
        catch (Exception ex)
        {
            LogEmailSendError(_logger, ex, to);
            // We don't throw here to avoid disrupting the user registration flow in case of SMTP misconfiguration
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "SMTP configuration is missing or invalid. Falling back to log-only email sender.")]
    static partial void LogSmtpConfigMissing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "--- DEV EMAIL SENDER ---\nTo: {Email}\nSubject: {Subject}\nBody: {Body}\n------------------------")]
    static partial void LogDevEmailSenderFallback(ILogger logger, string email, string subject, string body);

    [LoggerMessage(Level = LogLevel.Information, Message = "Email successfully sent to {Email} with subject {Subject}")]
    static partial void LogEmailSent(ILogger logger, string email, string subject);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while sending email to {Email}")]
    static partial void LogEmailSendError(ILogger logger, Exception ex, string email);
}
