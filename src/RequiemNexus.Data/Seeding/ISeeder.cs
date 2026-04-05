using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Ordered database seed step invoked during application startup (see <see cref="DbInitializer"/>).
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// Gets the execution order relative to other seeders (lower runs first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Applies this seed step to the database.
    /// </summary>
    /// <param name="context">The EF Core context (same instance for the full seed pipeline).</param>
    /// <param name="logger">Structured logger for seed diagnostics.</param>
    Task SeedAsync(ApplicationDbContext context, ILogger logger);
}
