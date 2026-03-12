namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Orchestrates JSON pack export and import across all homebrew entity types.
/// </summary>
public interface IHomebrewPackService
{
    /// <summary>
    /// Exports all homebrew content authored by <paramref name="userId"/> as an indented JSON string.
    /// </summary>
    /// <param name="userId">The user whose homebrew to export.</param>
    Task<string> ExportHomebrewPackAsync(string userId);

    /// <summary>
    /// Imports a homebrew pack from a JSON string, creating all entities scoped to <paramref name="userId"/>.
    /// Returns the total count of items imported.
    /// </summary>
    /// <param name="json">The JSON pack string to import.</param>
    /// <param name="userId">The user who will own the imported content.</param>
    Task<int> ImportHomebrewPackAsync(string json, string userId);
}
