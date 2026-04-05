using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Renders a character sheet as a PDF via QuestPDF (CPU-bound generation).
/// </summary>
public interface ICharacterPdfExportService
{
    /// <summary>Builds the PDF bytes synchronously (CPU-bound).</summary>
    byte[] ExportCharacterAsPdf(Character character);

    /// <summary>Loads the sheet by id and renders PDF on a background thread.</summary>
    Task<byte[]> ExportCharacterAsPdfAsync(int characterId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Renders on a background thread to avoid blocking the caller's sync context.</summary>
    Task<byte[]> ExportCharacterAsPdfAsync(Character character, CancellationToken cancellationToken = default);
}
