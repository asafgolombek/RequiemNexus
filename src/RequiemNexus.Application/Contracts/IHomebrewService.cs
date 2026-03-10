using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Service for managing homebrew content — custom Disciplines, Merits, and Clans/Bloodlines
/// scoped to a user. Also provides JSON pack export and import.
/// </summary>
public interface IHomebrewService
{
    // Homebrew Disciplines

    /// <summary>Returns all homebrew Disciplines authored by the given user.</summary>
    Task<List<Discipline>> GetHomebrewDisciplinesAsync(string userId);

    /// <summary>Creates a new homebrew Discipline scoped to the given user.</summary>
    Task<Discipline> CreateHomebrewDisciplineAsync(string name, string description, string userId);

    /// <summary>Deletes a homebrew Discipline. Only the author may delete.</summary>
    Task DeleteHomebrewDisciplineAsync(int disciplineId, string userId);

    // Homebrew Merits

    /// <summary>Returns all homebrew Merits authored by the given user.</summary>
    Task<List<Merit>> GetHomebrewMeritsAsync(string userId);

    /// <summary>Creates a new homebrew Merit scoped to the given user.</summary>
    Task<Merit> CreateHomebrewMeritAsync(string name, string description, string validRatings, bool requiresSpecification, string userId);

    /// <summary>Deletes a homebrew Merit. Only the author may delete.</summary>
    Task DeleteHomebrewMeritAsync(int meritId, string userId);

    // Homebrew Clans / Bloodlines

    /// <summary>Returns all homebrew Clans/Bloodlines authored by the given user.</summary>
    Task<List<Clan>> GetHomebrewClansAsync(string userId);

    /// <summary>Creates a new homebrew Clan/Bloodline scoped to the given user.</summary>
    Task<Clan> CreateHomebrewClanAsync(string name, string description, string userId);

    /// <summary>Deletes a homebrew Clan. Only the author may delete.</summary>
    Task DeleteHomebrewClanAsync(int clanId, string userId);

    // JSON Pack Export / Import

    /// <summary>
    /// Exports all homebrew content authored by <paramref name="userId"/> as a JSON pack string.
    /// </summary>
    Task<string> ExportHomebrewPackAsync(string userId);

    /// <summary>
    /// Imports a homebrew pack JSON string, creating content scoped to <paramref name="userId"/>.
    /// Returns the count of items imported.
    /// </summary>
    Task<int> ImportHomebrewPackAsync(string json, string userId);
}
