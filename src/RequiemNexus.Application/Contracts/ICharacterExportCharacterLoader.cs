using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Loads a <see cref="Character"/> graph for export when the caller owns the sheet (<see cref="Character.ApplicationUserId"/> match).
/// </summary>
public interface ICharacterExportCharacterLoader
{
    /// <summary>
    /// Loads the character with all related rows required for JSON/PDF export, or null when not found or not owned.
    /// </summary>
    /// <param name="characterId">Primary key.</param>
    /// <param name="userId">Authenticated application user id.</param>
    /// <param name="cancellationToken">Optional cancellation.</param>
    /// <returns>The tracked character graph or null.</returns>
    Task<Character?> LoadOwnedCharacterAsync(int characterId, string userId, CancellationToken cancellationToken = default);
}
