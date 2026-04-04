using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Character persistence and progression mutations.
/// </summary>
public interface ICharacterWriter
{
    Task DeleteCharacterAsync(int id, string userId);

    Task<Character> EmbraceCharacterAsync(Character newCharacter);

    Task SaveAsync(Character character);

    /// <returns>Beats and XP totals after the mutation (for patching UI without a full reload).</returns>
    Task<CharacterProgressionSnapshotDto> AddBeatAsync(int characterId, string userId);

    /// <returns>Beats and XP totals after the mutation.</returns>
    Task<CharacterProgressionSnapshotDto> RemoveBeatAsync(int characterId, string userId);

    /// <returns>Beats and XP totals after the mutation.</returns>
    Task<CharacterProgressionSnapshotDto> AddXPAsync(int characterId, string userId);

    /// <returns>Beats and XP totals after the mutation.</returns>
    Task<CharacterProgressionSnapshotDto> RemoveXPAsync(int characterId, string userId);

    /// <summary>Retires the character from campaign play. Owner or campaign ST may call this.</summary>
    Task RetireCharacterAsync(int characterId, string userId);

    /// <summary>Un-retires the character, returning it to active campaign play. Owner or campaign ST may call this.</summary>
    Task UnretireCharacterAsync(int characterId, string userId);

    /// <summary>Archives the character globally, hiding it from the active character list. Owner only.</summary>
    Task ArchiveCharacterAsync(int characterId, string userId);

    /// <summary>Un-archives the character, restoring it to the active list. Owner only.</summary>
    Task UnarchiveCharacterAsync(int characterId, string userId);
}
