using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Serializes character sheet data as JSON (export / GDPR-style dumps).
/// </summary>
public interface ICharacterJsonExportService
{
    /// <summary>Serializes the in-memory character graph to indented JSON (CPU-bound).</summary>
    string ExportCharacterAsJson(Character character);

    /// <summary>Loads the sheet by id and exports as JSON; uses a background thread for serialization.</summary>
    Task<string> ExportCharacterAsJsonAsync(int characterId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Serializes on a background thread to avoid blocking the caller's sync context (e.g. Blazor render).</summary>
    Task<string> ExportCharacterAsJsonAsync(Character character, CancellationToken cancellationToken = default);
}
