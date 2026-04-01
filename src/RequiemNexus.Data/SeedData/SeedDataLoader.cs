using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Provides a single entry-point for reading JSON seed files from the SeedSource directory.
/// Centralises the directory-check / file-exists / try-catch-log pattern used by every seeder.
/// </summary>
public static class SeedDataLoader
{
    /// <summary>
    /// Reads and parses a JSON seed file from the SeedSource directory.
    /// Returns <c>null</c> (and logs an error) when the directory is not found,
    /// the file does not exist, or the JSON is malformed.
    /// Callers are responsible for disposing the returned <see cref="JsonDocument"/>.
    /// </summary>
    public static JsonDocument? TryLoadJson(string fileName, ILogger logger)
    {
        string? seedDir = SeedSourcePathResolver.GetSeedDirectory();
        if (seedDir == null)
        {
            return null;
        }

        string path = Path.Combine(seedDir, fileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var reader = new StreamReader(
                path,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                detectEncodingFromByteOrderMarks: true);
            string json = reader.ReadToEnd();
            return JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to parse {FileName}; falling back to in-memory seed. Verify JSON integrity.",
                fileName);
            return null;
        }
    }
}
