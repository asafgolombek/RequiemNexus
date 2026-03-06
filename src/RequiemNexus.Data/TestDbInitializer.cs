using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data;

/// <summary>
/// Seeds repeatable, deterministic data for integration and E2E tests.
/// </summary>
public static class TestDbInitializer
{
    public const string TestUserEmail = "e2etest@requiemnexus.local";
#pragma warning disable S2068 // "password" detected here, but this is a deterministic test-only credential
    public const string TestUserPassword = "test"; // Used in E2E tests
#pragma warning restore S2068


    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Define a test user
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == TestUserEmail);
        if (user == null)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            var newUser = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = TestUserEmail,
                NormalizedUserName = TestUserEmail.ToUpperInvariant(),
                Email = TestUserEmail,
                NormalizedEmail = TestUserEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            newUser.PasswordHash = hasher.HashPassword(newUser, TestUserPassword);

            await context.Users.AddAsync(newUser);
            await context.SaveChangesAsync();

        }
    }
}
