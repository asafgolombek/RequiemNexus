using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface ICharacterExportService
{
    Task<string> ExportCharacterAsJsonAsync(int characterId);

    string ExportCharacterAsJson(Character character);

    byte[] ExportCharacterAsPdf(Character character);

    Task<byte[]> ExportCharacterAsPdfAsync(int characterId);
}
