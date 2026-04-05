using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Facade for character export — delegates JSON and PDF generation to dedicated services.
/// </summary>
public sealed class CharacterExportService(
    ICharacterJsonExportService jsonExport,
    ICharacterPdfExportService pdfExport) : ICharacterExportService
{
    private readonly ICharacterJsonExportService _jsonExport = jsonExport;
    private readonly ICharacterPdfExportService _pdfExport = pdfExport;

    /// <inheritdoc />
    public Task<string> ExportCharacterAsJsonAsync(int characterId, string userId) =>
        _jsonExport.ExportCharacterAsJsonAsync(characterId, userId);

    /// <inheritdoc />
    public string ExportCharacterAsJson(Character character) =>
        _jsonExport.ExportCharacterAsJson(character);

    /// <inheritdoc />
    public Task<string> ExportCharacterAsJsonAsync(Character character) =>
        _jsonExport.ExportCharacterAsJsonAsync(character);

    /// <inheritdoc />
    public byte[] ExportCharacterAsPdf(Character character) =>
        _pdfExport.ExportCharacterAsPdf(character);

    /// <inheritdoc />
    public Task<byte[]> ExportCharacterAsPdfAsync(int characterId, string userId) =>
        _pdfExport.ExportCharacterAsPdfAsync(characterId, userId);

    /// <inheritdoc />
    public Task<byte[]> ExportCharacterAsPdfAsync(Character character) =>
        _pdfExport.ExportCharacterAsPdfAsync(character);
}
