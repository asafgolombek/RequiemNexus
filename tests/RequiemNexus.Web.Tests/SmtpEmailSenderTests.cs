using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Web.Services;
using Xunit;

namespace RequiemNexus.Web.Tests;

public class SmtpEmailSenderTests
{
    private readonly Mock<ILogger<SmtpEmailSender>> _loggerMock;
    private readonly IConfiguration _configuration;

    public SmtpEmailSenderTests()
    {
        _loggerMock = new Mock<ILogger<SmtpEmailSender>>();
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var inMemorySettings = new Dictionary<string, string?> {
            {"Smtp:Host", "localhost"},
            {"Smtp:Port", "587"},
            {"Smtp:Username", "user"},
            {"Smtp:Password", "pass"},
            {"Smtp:From", "noreply@example.com"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task SendEmailAsync_WithPlaceholderCredentials_LogsWarningAndFallsBack()
    {
        // Arrange
        var settings = new Dictionary<string, string?> {
            {"Smtp:Host", "localhost"},
            {"Smtp:Port", "587"},
            {"Smtp:Username", "SetInUserSecrets"},
            {"Smtp:Password", "SetInUserSecrets"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var sender = new SmtpEmailSender(_loggerMock.Object, config);

        // Act
        await sender.SendConfirmationLinkAsync(null!, "test@example.com", "http://confirm");

        // Assert
        VerifyLogContains(_loggerMock, LogLevel.Warning, "SMTP credentials are set to placeholder values");
        VerifyLogContains(_loggerMock, LogLevel.Information, "--- DEV EMAIL SENDER ---");
    }

    [Fact]
    public async Task SendEmailAsync_WithMissingConfig_LogsWarningAndFallsBack()
    {
        // Arrange
        var settings = new Dictionary<string, string?> {
            {"Smtp:Host", ""}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var sender = new SmtpEmailSender(_loggerMock.Object, config);

        // Act
        await sender.SendConfirmationLinkAsync(null!, "test@example.com", "http://confirm");

        // Assert
        VerifyLogContains(_loggerMock, LogLevel.Warning, "SMTP configuration is missing or invalid");
        VerifyLogContains(_loggerMock, LogLevel.Information, "--- DEV EMAIL SENDER ---");
    }

    [Fact]
    public async Task SendEmailAsync_OnException_LogsErrorAndFallsBack()
    {
        // Arrange
        var settings = new Dictionary<string, string?> {
            {"Smtp:Host", "invalid.host"}, // This will cause SmtpClient to throw eventually, 
                                           // but wait, SmtpClient is hard to trigger immediately 
                                           // without actual network attempt or mock.
                                           // However, our code catches ANY exception.
            {"Smtp:Port", "587"}
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var sender = new SmtpEmailSender(_loggerMock.Object, config);

        // Act
        // This will likely throw SocketException or SmtpException inside SendMailAsync
        await sender.SendConfirmationLinkAsync(null!, "test@example.com", "http://confirm");

        // Assert
        VerifyLogContains(_loggerMock, LogLevel.Error, "An error occurred while sending email");
        VerifyLogContains(_loggerMock, LogLevel.Information, "--- DEV EMAIL SENDER ---");
    }

    private void VerifyLogContains<T>(Mock<ILogger<T>> loggerMock, LogLevel level, string expectedMessage)
    {
        loggerMock.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
