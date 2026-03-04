using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class IdentityIntegrationTests
{
    private static ServiceProvider CreateServiceProvider(string dbName)
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        services.AddLogging();

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task UserManager_AccessFailed_LocksOutUserAfterMaxAttempts()
    {
        // Arrange
        var provider = CreateServiceProvider(nameof(UserManager_AccessFailed_LocksOutUserAfterMaxAttempts));
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser { UserName = "test@example.com", Email = "test@example.com" };
        var createResult = await userManager.CreateAsync(user, "StrongPass123!");
        Assert.True(createResult.Succeeded);

        user = await userManager.FindByEmailAsync("test@example.com");
        Assert.NotNull(user);

        // Act & Assert
        // Fail 4 times
        for (int i = 0; i < 4; i++)
        {
            await userManager.AccessFailedAsync(user);
            Assert.False(await userManager.IsLockedOutAsync(user));
        }

        // Fail 5th time
        await userManager.AccessFailedAsync(user);

        // Now it should be locked out
        Assert.True(await userManager.IsLockedOutAsync(user));
        Assert.NotNull(user.LockoutEnd);
        Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow);
    }
}
