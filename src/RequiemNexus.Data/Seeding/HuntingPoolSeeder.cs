using Microsoft.Extensions.Logging;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Seeds hunting pool definitions.
/// </summary>
public sealed class HuntingPoolSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 30;

    /// <inheritdoc />
    public Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return HuntingPoolDefinitionSeedData.SeedAsync(context);
    }
}
