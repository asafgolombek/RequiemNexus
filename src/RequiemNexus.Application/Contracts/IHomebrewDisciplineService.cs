using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages user-scoped homebrew Disciplines.
/// </summary>
public interface IHomebrewDisciplineService
{
    /// <summary>Returns all homebrew Disciplines authored by the given user.</summary>
    /// <param name="userId">The user whose disciplines to return.</param>
    Task<List<Discipline>> GetHomebrewDisciplinesAsync(string userId);

    /// <summary>Creates a new homebrew Discipline scoped to the given user.</summary>
    /// <param name="name">The display name of the discipline.</param>
    /// <param name="description">A short description.</param>
    /// <param name="userId">The author (owner) of this homebrew entry.</param>
    Task<Discipline> CreateHomebrewDisciplineAsync(string name, string description, string userId);

    /// <summary>Deletes a homebrew Discipline. Only the author may delete.</summary>
    /// <param name="disciplineId">The discipline to delete.</param>
    /// <param name="userId">The requesting user (must be the author).</param>
    Task DeleteHomebrewDisciplineAsync(int disciplineId, string userId);
}
