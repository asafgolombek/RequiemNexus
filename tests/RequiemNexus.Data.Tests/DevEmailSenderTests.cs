using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Services;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class DevEmailSenderTests
{
    [Fact]
    public async Task SendConfirmationLinkAsync_DoesNotThrow()
    {
        // Arrange
        var logger = NullLogger<DevEmailSender>.Instance;
        var sender = new DevEmailSender(logger);
        var user = new ApplicationUser { Email = "test@example.com" };

        // Act
        var exception = await Record.ExceptionAsync(() =>
            sender.SendConfirmationLinkAsync(user, "test@example.com", "http://localhost/confirm"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendPasswordResetCodeAsync_DoesNotThrow()
    {
        // Arrange
        var logger = NullLogger<DevEmailSender>.Instance;
        var sender = new DevEmailSender(logger);
        var user = new ApplicationUser { Email = "test@example.com" };

        // Act
        var exception = await Record.ExceptionAsync(() =>
            sender.SendPasswordResetCodeAsync(user, "test@example.com", "123456"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendPasswordResetLinkAsync_DoesNotThrow()
    {
        // Arrange
        var logger = NullLogger<DevEmailSender>.Instance;
        var sender = new DevEmailSender(logger);
        var user = new ApplicationUser { Email = "test@example.com" };

        // Act
        var exception = await Record.ExceptionAsync(() =>
            sender.SendPasswordResetLinkAsync(user, "test@example.com", "http://localhost/reset"));

        // Assert
        Assert.Null(exception);
    }
}
