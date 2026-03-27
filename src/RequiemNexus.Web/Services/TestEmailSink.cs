using System.Collections.Concurrent;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Dtos;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Captures identity emails in memory when <c>ASPNETCORE_ENVIRONMENT=Testing</c>.
/// Must not be registered outside the Testing environment.
/// </summary>
public sealed class TestEmailSink : IRequiemEmailService
{
    private readonly ConcurrentQueue<EmailCapture> _messages = new();

    /// <summary>
    /// Gets a snapshot of captured messages (newest last).
    /// </summary>
    /// <returns>Read-only list of captures.</returns>
    public IReadOnlyList<EmailCapture> GetCapturedMessages() => _messages.ToArray();

    /// <summary>
    /// Removes all captured messages (for test isolation).
    /// </summary>
    public void Clear() => _messages.Clear();

    /// <inheritdoc />
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        _messages.Enqueue(new EmailCapture(email, "Confirm your email", confirmationLink));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        _messages.Enqueue(new EmailCapture(email, "Reset your password", resetCode));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        _messages.Enqueue(new EmailCapture(email, "Reset your password", resetLink));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendEmailChangeLinkAsync(ApplicationUser user, string newEmail, string changeLink)
    {
        _messages.Enqueue(new EmailCapture(newEmail, "Confirm your new email address", changeLink));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendAccountRecoveryCodeAsync(ApplicationUser user, string email, string code)
    {
        _messages.Enqueue(new EmailCapture(email, "Requiem Nexus Account Recovery", code));
        return Task.CompletedTask;
    }
}
