using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Beat and experience mutations with snapshot results for UI patching (see <see cref="CharacterProgressionSnapshotDto"/>).
/// </summary>
public interface ICharacterProgressionService
{
    /// <summary>Adds one Beat, applies beat-to-XP conversion rules, records ledger entries, and broadcasts session updates.</summary>
    /// <returns>Beats and XP totals after the mutation.</returns>
    Task<CharacterProgressionSnapshotDto> AddBeatAsync(int characterId, string userId);

    /// <summary>Removes one Beat if above zero, saves, and broadcasts.</summary>
    /// <returns>Beats and XP totals after the mutation.</returns>
    Task<CharacterProgressionSnapshotDto> RemoveBeatAsync(int characterId, string userId);

    /// <summary>Adds one XP (manual), records ledger, saves, and broadcasts.</summary>
    /// <returns>Beats and XP totals after the mutation.</returns>
    Task<CharacterProgressionSnapshotDto> AddXPAsync(int characterId, string userId);

    /// <summary>Removes one XP if above zero, records ledger spend, saves, and broadcasts.</summary>
    /// <returns>Beats and XP totals after the mutation.</returns>
    Task<CharacterProgressionSnapshotDto> RemoveXPAsync(int characterId, string userId);
}
