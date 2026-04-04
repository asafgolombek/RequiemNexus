using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Read-only character queries (lists, access-checked snapshots, ritual targets).
/// Mutations and tracked edit loads remain on <see cref="ICharacterWriter"/>, <see cref="ICharacterProgressionService"/>, and <see cref="ICharacterService"/>.
/// </summary>
public interface ICharacterQueryService
{
    /// <summary>Active (non-archived) characters owned by the user.</summary>
    Task<List<Character>> GetCharactersByUserIdAsync(string userId);

    /// <summary>Archived characters owned by the user.</summary>
    Task<List<Character>> GetArchivedCharactersAsync(string userId);

    /// <summary>
    /// Returns a no-tracking character snapshot with an access flag for owner vs campaign member.
    /// </summary>
    Task<(Character Character, bool IsOwner)?> GetCharacterWithAccessCheckAsync(int characterId, string requestingUserId);

    /// <summary>
    /// Returns other vampires in the same campaign as the character for ritual Blood Sympathy targeting.
    /// </summary>
    /// <remarks>
    /// Caller must verify authorization (e.g. character owner) before invoking; this method performs no auth.
    /// </remarks>
    Task<IReadOnlyList<CampaignKindredTargetDto>> GetCampaignKindredTargetsForRitesAsync(int characterId);
}
