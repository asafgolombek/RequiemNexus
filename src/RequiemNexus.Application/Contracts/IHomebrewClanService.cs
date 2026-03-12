using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages user-scoped homebrew Clans and Bloodlines.
/// </summary>
public interface IHomebrewClanService
{
    /// <summary>Returns all homebrew Clans/Bloodlines authored by the given user.</summary>
    /// <param name="userId">The user whose clans to return.</param>
    Task<List<Clan>> GetHomebrewClansAsync(string userId);

    /// <summary>Creates a new homebrew Clan/Bloodline scoped to the given user.</summary>
    /// <param name="name">The display name of the clan.</param>
    /// <param name="description">A short description.</param>
    /// <param name="userId">The author (owner) of this homebrew entry.</param>
    Task<Clan> CreateHomebrewClanAsync(string name, string description, string userId);

    /// <summary>Deletes a homebrew Clan/Bloodline. Only the author may delete.</summary>
    /// <param name="clanId">The clan to delete.</param>
    /// <param name="userId">The requesting user (must be the author).</param>
    Task DeleteHomebrewClanAsync(int clanId, string userId);
}
