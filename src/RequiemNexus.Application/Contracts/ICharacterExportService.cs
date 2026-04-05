using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Character sheet export (JSON + PDF). Implemented as a thin facade over <see cref="ICharacterJsonExportService"/> and <see cref="ICharacterPdfExportService"/>.
/// </summary>
public interface ICharacterExportService
{
    /// <summary>Exports a character as JSON. Requires <paramref name="userId"/> to own the sheet.</summary>
    Task<string> ExportCharacterAsJsonAsync(int characterId, string userId);

    /// <summary>Serializes an in-memory character graph to JSON (CPU work on the calling thread).</summary>
    string ExportCharacterAsJson(Character character);

    /// <summary>Serializes on a background thread (preferred from Blazor UI).</summary>
    Task<string> ExportCharacterAsJsonAsync(Character character);

    /// <summary>Builds PDF bytes synchronously (CPU-bound).</summary>
    byte[] ExportCharacterAsPdf(Character character);

    /// <summary>Exports a character as PDF. Requires <paramref name="userId"/> to own the sheet.</summary>
    Task<byte[]> ExportCharacterAsPdfAsync(int characterId, string userId);

    /// <summary>Builds PDF on a background thread (preferred from Blazor UI).</summary>
    Task<byte[]> ExportCharacterAsPdfAsync(Character character);
}
