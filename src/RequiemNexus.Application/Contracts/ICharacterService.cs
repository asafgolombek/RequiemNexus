using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface ICharacterService
{
    Task<List<Character>> GetCharactersByUserIdAsync(string userId);

    /// <summary>Returns archived characters owned by the given user.</summary>
    Task<List<Character>> GetArchivedCharactersAsync(string userId);

    Task<Character?> GetCharacterByIdAsync(int id, string userId);

    /// <summary>
    /// Reloads the character from the database, bypassing EF tracking cache.
    /// Use after external mutations (e.g. bloodline removal) to get fresh navigation properties.
    /// </summary>
    Task<Character?> ReloadCharacterAsync(int id, string userId);

    Task DeleteCharacterAsync(int id, string userId);

    Task<Character> EmbraceCharacterAsync(Character newCharacter);

    // Persistence
    Task SaveAsync(Character character);

    // Beat & XP mutations
    Task AddBeatAsync(int characterId, string userId);

    Task RemoveBeatAsync(int characterId, string userId);

    Task AddXPAsync(int characterId, string userId);

    Task RemoveXPAsync(int characterId, string userId);

    // Retirement (campaign-scoped)

    /// <summary>Retires the character from campaign play. Owner or campaign ST may call this.</summary>
    Task RetireCharacterAsync(int characterId, string userId);

    /// <summary>Un-retires the character, returning it to active campaign play. Owner or campaign ST may call this.</summary>
    Task UnretireCharacterAsync(int characterId, string userId);

    // Archiving (global)

    /// <summary>Archives the character globally, hiding it from the active character list. Owner only.</summary>
    Task ArchiveCharacterAsync(int characterId, string userId);

    /// <summary>Un-archives the character, restoring it to the active list. Owner only.</summary>
    Task UnarchiveCharacterAsync(int characterId, string userId);

    /// <summary>
    /// Returns the fully-loaded character together with an access-level flag.
    /// <list type="bullet">
    ///   <item>Returns <c>(Character, true)</c> when <paramref name="requestingUserId"/> is the character's owner.</item>
    ///   <item>Returns <c>(Character, false)</c> when the user is a member of the character's campaign (Storyteller or fellow player) — read-only access.</item>
    ///   <item>Returns <c>null</c> when the user has no access.</item>
    /// </list>
    /// </summary>
    Task<(Character Character, bool IsOwner)?> GetCharacterWithAccessCheckAsync(int characterId, string requestingUserId);

    /// <summary>
    /// Returns other vampires in the same chronicle as the character for optional Blood Sympathy ritual targeting.
    /// </summary>
    /// <param name="characterId">The ritualist's character.</param>
    /// <param name="userId">The authenticated user (must own the character).</param>
    Task<IReadOnlyList<CampaignKindredTargetDto>> GetCampaignKindredTargetsForRitesAsync(int characterId, string userId);
}
