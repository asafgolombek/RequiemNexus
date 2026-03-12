using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages user-scoped homebrew Merits.
/// </summary>
public interface IHomebrewMeritService
{
    /// <summary>Returns all homebrew Merits authored by the given user.</summary>
    /// <param name="userId">The user whose merits to return.</param>
    Task<List<Merit>> GetHomebrewMeritsAsync(string userId);

    /// <summary>Creates a new homebrew Merit scoped to the given user.</summary>
    /// <param name="name">The display name of the merit.</param>
    /// <param name="description">A short description.</param>
    /// <param name="validRatings">Comma-separated list of purchasable dot ratings (e.g. "1,2,3").</param>
    /// <param name="requiresSpecification">Whether the merit requires a free-text specifier.</param>
    /// <param name="userId">The author (owner) of this homebrew entry.</param>
    Task<Merit> CreateHomebrewMeritAsync(string name, string description, string validRatings, bool requiresSpecification, string userId);

    /// <summary>Deletes a homebrew Merit. Only the author may delete.</summary>
    /// <param name="meritId">The merit to delete.</param>
    /// <param name="userId">The requesting user (must be the author).</param>
    Task DeleteHomebrewMeritAsync(int meritId, string userId);
}
