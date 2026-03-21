using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequiemNexus.Data;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Builds <see cref="IDbContextFactory{TContext}"/> for integration tests so <see cref="Application.Services.AuthorizationHelper"/>
/// matches production (factory-per-operation) while sharing the same in-memory database name as a manually created context.
/// </summary>
internal static class InMemoryApplicationDbContextFactories
{
    /// <summary>
    /// Returns a factory whose contexts use the same in-memory store as <paramref name="databaseName"/>.
    /// </summary>
    public static IDbContextFactory<ApplicationDbContext> ForDatabaseName(string databaseName)
    {
        ServiceCollection services = new();
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    }
}
