using Microsoft.Extensions.Configuration;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Tests;

/// <summary>
/// Tests for <see cref="ExternalAuthAvailability"/>.
/// </summary>
public class ExternalAuthAvailabilityTests
{
    [Fact]
    public void IsDiscordEnabled_false_when_client_id_is_placeholder()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Discord:ClientId"] = "not-configured",
                ["Authentication:Discord:ClientSecret"] = "real-secret",
            })
            .Build();

        var sut = new ExternalAuthAvailability(config);

        Assert.False(sut.IsDiscordEnabled);
    }

    [Fact]
    public void IsGoogleEnabled_true_when_both_credentials_are_non_placeholder()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "abc.apps.googleusercontent.com",
                ["Authentication:Google:ClientSecret"] = "secret-value",
            })
            .Build();

        var sut = new ExternalAuthAvailability(config);

        Assert.True(sut.IsGoogleEnabled);
    }

    [Fact]
    public void IsDiscordEnabled_true_when_case_insensitive_not_configured_rejected_on_secret()
    {
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Discord:ClientId"] = "123",
                ["Authentication:Discord:ClientSecret"] = "NOT-CONFIGURED",
            })
            .Build();

        var sut = new ExternalAuthAvailability(config);

        Assert.False(sut.IsDiscordEnabled);
    }
}
