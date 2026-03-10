using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class ProfileManagementTests
{
    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddLogging();
        services.AddDataProtection();

        services.AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task UpdateAsync_PersistsDisplayNameAndAvatarUrl()
    {
        // Arrange
        var provider = CreateServiceProvider(nameof(UpdateAsync_PersistsDisplayNameAndAvatarUrl));
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = "profile@example.com", Email = "profile@example.com" };
        await userManager.CreateAsync(user, "StrongPass123!");

        // Act
        user.DisplayName = "Night Prince";
        user.AvatarUrl = "https://example.com/avatar.png";
        var result = await userManager.UpdateAsync(user);

        // Assert
        Assert.True(result.Succeeded);

        var reloaded = await userManager.FindByEmailAsync("profile@example.com");
        Assert.NotNull(reloaded);
        Assert.Equal("Night Prince", reloaded.DisplayName);
        Assert.Equal("https://example.com/avatar.png", reloaded.AvatarUrl);
    }

    [Fact]
    public async Task UpdateAsync_AllowsNullDisplayNameAndAvatarUrl()
    {
        // Arrange
        var provider = CreateServiceProvider(nameof(UpdateAsync_AllowsNullDisplayNameAndAvatarUrl));
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = "nullable@example.com",
            Email = "nullable@example.com",
            DisplayName = "Old Name",
            AvatarUrl = "https://example.com/old.png",
        };
        await userManager.CreateAsync(user, "StrongPass123!");

        // Act – clear both fields
        user.DisplayName = null;
        user.AvatarUrl = null;
        var result = await userManager.UpdateAsync(user);

        // Assert
        Assert.True(result.Succeeded);

        var reloaded = await userManager.FindByEmailAsync("nullable@example.com");
        Assert.NotNull(reloaded);
        Assert.Null(reloaded.DisplayName);
        Assert.Null(reloaded.AvatarUrl);
    }

    [Fact]
    public async Task GenerateChangeEmailTokenAsync_ProducesNonEmptyToken()
    {
        // Arrange
        var provider = CreateServiceProvider(nameof(GenerateChangeEmailTokenAsync_ProducesNonEmptyToken));
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = "tokentest@example.com", Email = "tokentest@example.com" };
        await userManager.CreateAsync(user, "StrongPass123!");

        // Act
        var token = await userManager.GenerateChangeEmailTokenAsync(user, "new@example.com");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task ChangeEmailAsync_WithValidToken_UpdatesEmail()
    {
        // Arrange
        var provider = CreateServiceProvider(nameof(ChangeEmailAsync_WithValidToken_UpdatesEmail));
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = "old@example.com", Email = "old@example.com" };
        await userManager.CreateAsync(user, "StrongPass123!");

        const string newEmail = "updated@example.com";
        var token = await userManager.GenerateChangeEmailTokenAsync(user, newEmail);

        // Act
        var result = await userManager.ChangeEmailAsync(user, newEmail, token);
        await userManager.SetUserNameAsync(user, newEmail);

        // Assert
        Assert.True(result.Succeeded);

        var reloaded = await userManager.FindByEmailAsync(newEmail);
        Assert.NotNull(reloaded);
        Assert.Equal(newEmail, reloaded.Email);
        Assert.Equal(newEmail, reloaded.UserName);
    }

    [Fact]
    public async Task ChangeEmailAsync_WithInvalidToken_Fails()
    {
        // Arrange
        var provider = CreateServiceProvider(nameof(ChangeEmailAsync_WithInvalidToken_Fails));
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = "badtoken@example.com", Email = "badtoken@example.com" };
        await userManager.CreateAsync(user, "StrongPass123!");

        // Act
        var result = await userManager.ChangeEmailAsync(user, "other@example.com", "this-is-not-a-valid-token");

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public void AuditEventType_ContainsProfileManagementEvents()
    {
        Assert.True(Enum.IsDefined(typeof(AuditEventType), AuditEventType.DisplayNameChanged));
        Assert.True(Enum.IsDefined(typeof(AuditEventType), AuditEventType.EmailChangeRequested));
        Assert.True(Enum.IsDefined(typeof(AuditEventType), AuditEventType.EmailChanged));
    }
}
