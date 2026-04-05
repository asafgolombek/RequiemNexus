using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Seeding;

namespace RequiemNexus.Data;

/// <summary>
/// Orchestrates EF migrations (optional), Identity roles, and ordered <see cref="ISeeder"/> steps.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Migrates (when requested), seeds roles, then runs all <paramref name="seeders"/> in <see cref="ISeeder.Order"/> sequence.
    /// </summary>
    /// <param name="context">Database context.</param>
    /// <param name="roleManager">ASP.NET Core Identity role manager.</param>
    /// <param name="logger">Logger for the seed pipeline.</param>
    /// <param name="seeders">Registered seed steps (typically from DI: <c>GetServices&lt;ISeeder&gt;()</c>).</param>
    /// <param name="runMigrations">When <see langword="true"/>, applies pending EF Core migrations before seeding when the provider is relational (skipped for InMemory and other non-relational stores).</param>
    public static async Task InitializeAsync(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager,
        ILogger logger,
        IEnumerable<ISeeder> seeders,
        bool runMigrations = false)
    {
        if (runMigrations && context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }

        await SeedRolesAsync(roleManager);

        foreach (ISeeder seeder in seeders.OrderBy(s => s.Order))
        {
            await seeder.SeedAsync(context, logger);
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Player", "Storyteller", "Admin"];
        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
