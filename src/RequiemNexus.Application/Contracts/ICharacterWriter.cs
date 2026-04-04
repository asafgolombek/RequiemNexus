using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Character persistence mutations (excluding beat/XP — see <see cref="ICharacterProgressionService"/>).
/// </summary>
public interface ICharacterWriter
{
    Task DeleteCharacterAsync(int id, string userId);

    Task<Character> EmbraceCharacterAsync(Character newCharacter);

    Task SaveAsync(Character character);

    /// <summary>Retires the character from campaign play. Owner or campaign ST may call this.</summary>
    Task RetireCharacterAsync(int characterId, string userId);

    /// <summary>Un-retires the character, returning it to active campaign play. Owner or campaign ST may call this.</summary>
    Task UnretireCharacterAsync(int characterId, string userId);

    /// <summary>Archives the character globally, hiding it from the active character list. Owner only.</summary>
    Task ArchiveCharacterAsync(int characterId, string userId);

    /// <summary>Un-archives the character, restoring it to the active list. Owner only.</summary>
    Task UnarchiveCharacterAsync(int characterId, string userId);
}
