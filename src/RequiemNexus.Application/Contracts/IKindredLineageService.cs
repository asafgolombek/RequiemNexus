using RequiemNexus.Application.DTOs;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Storyteller-managed Kindred lineage and Blood Sympathy rolls for a chronicle.
/// </summary>
public interface IKindredLineageService
{
    /// <summary>Links a sire (PC) to a character. Replaces any existing sire link.</summary>
    /// <param name="characterId">The character whose sire is set.</param>
    /// <param name="sireCharacterId">The sire PC.</param>
    /// <param name="userId">The authenticated user (must be Storyteller).</param>
    /// <returns>Failure when validation fails; success when persisted.</returns>
    Task<Result<Unit>> SetSireCharacterAsync(int characterId, int sireCharacterId, string userId);

    /// <summary>Links an NPC sire to a character.</summary>
    Task<Result<Unit>> SetSireNpcAsync(int characterId, int sireNpcId, string userId);

    /// <summary>Sets a free-text sire name for unlinked / external sires.</summary>
    Task<Result<Unit>> SetSireDisplayNameAsync(int characterId, string? name, string userId);

    /// <summary>Clears all sire linkage from a character.</summary>
    Task<Result<Unit>> ClearSireAsync(int characterId, string userId);

    /// <summary>Returns the lineage graph (sire + childer) for a character.</summary>
    Task<LineageGraphDto> GetLineageGraphAsync(int characterId, string userId);
}
