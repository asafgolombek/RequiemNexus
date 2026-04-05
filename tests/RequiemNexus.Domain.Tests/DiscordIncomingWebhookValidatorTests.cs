using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class DiscordIncomingWebhookValidatorTests
{
    [Fact]
    public void Validate_Empty_ReturnsNull()
    {
        Result<string?> r = DiscordIncomingWebhookValidator.Validate(null);
        Assert.True(r.IsSuccess);
        Assert.Null(r.Value);

        r = DiscordIncomingWebhookValidator.Validate("   ");
        Assert.True(r.IsSuccess);
        Assert.Null(r.Value);
    }

    [Fact]
    public void Validate_GoodDiscordUrl_Succeeds()
    {
        const string url = "https://discord.com/api/webhooks/1234567890/abcdefghijklmnopqrstuvwxyz";
        Result<string?> r = DiscordIncomingWebhookValidator.Validate(url);
        Assert.True(r.IsSuccess);
        Assert.Equal(url, r.Value);
    }

    [Fact]
    public void Validate_DiscordAppHost_Succeeds()
    {
        const string url = "https://discordapp.com/api/webhooks/1/tok";
        Result<string?> r = DiscordIncomingWebhookValidator.Validate(url);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void Validate_Http_Fails()
    {
        Result<string?> r = DiscordIncomingWebhookValidator.Validate("http://discord.com/api/webhooks/1/tok");
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public void Validate_WrongHost_Fails()
    {
        Result<string?> r = DiscordIncomingWebhookValidator.Validate("https://evil.com/api/webhooks/1/tok");
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public void Validate_WrongPath_Fails()
    {
        Result<string?> r = DiscordIncomingWebhookValidator.Validate("https://discord.com/api/v10/channels/1");
        Assert.False(r.IsSuccess);
    }
}
