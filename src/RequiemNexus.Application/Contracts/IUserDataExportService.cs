namespace RequiemNexus.Application.Contracts;

public interface IUserDataExportService
{
    /// <summary>
    /// Exports all personal data for a user as a JSON string.
    /// Covers GDPR Article 20 (data portability) and CCPA right to know.
    /// </summary>
    Task<string> ExportUserDataAsJsonAsync(string userId);
}
