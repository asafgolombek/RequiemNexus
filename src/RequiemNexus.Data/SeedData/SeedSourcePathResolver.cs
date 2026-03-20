namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Resolves the path to the SeedSource directory containing JSON seed files.
/// Probes multiple locations for dev, published app, and Docker deployment.
/// </summary>
public static class SeedSourcePathResolver
{
    /// <summary>
    /// Returns the SeedSource directory path, or null if not found.
    /// </summary>
    public static string? GetSeedDirectory()
    {
        var basePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "SeedSource"),
            Path.Combine(Directory.GetCurrentDirectory(), "SeedSource"),
            Path.Combine(AppContext.BaseDirectory, "..", "SeedSource"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "SeedSource"),
        };

        return basePaths.FirstOrDefault(Directory.Exists);
    }
}
