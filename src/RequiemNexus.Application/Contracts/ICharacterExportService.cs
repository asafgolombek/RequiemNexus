using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface ICharacterExportService
{
    /// <summary>Exports a character as JSON. Requires <paramref name="userId"/> to own or have Storyteller access.</summary>
    Task<string> ExportCharacterAsJsonAsync(int characterId, string userId);

    string ExportCharacterAsJson(Character character);

    byte[] ExportCharacterAsPdf(Character character);

    /// <summary>Exports a character as PDF. Requires <paramref name="userId"/> to own or have Storyteller access.</summary>
    Task<byte[]> ExportCharacterAsPdfAsync(int characterId, string userId);
}
