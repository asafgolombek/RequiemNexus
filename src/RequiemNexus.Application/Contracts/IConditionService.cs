using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages Conditions and Tilts for characters.
/// All mutations verify that the requesting user is the character's owner or the campaign Storyteller.
/// </summary>
public interface IConditionService
{
    /// <summary>
    /// Applies a Condition to a character.
    /// The caller must be the character's owner or the campaign Storyteller.
    /// </summary>
    Task<CharacterCondition> ApplyConditionAsync(
        int characterId,
        ConditionType type,
        string? customName,
        string? descriptionOverride,
        string userId);

    /// <summary>
    /// Resolves an active Condition.
    /// If the Condition awards a Beat, a <c>BeatLedgerEntry</c> is written automatically.
    /// The caller must be the character's owner or the campaign Storyteller.
    /// </summary>
    Task ResolveConditionAsync(int conditionId, string userId);

    /// <summary>Returns all Conditions for a character (active and resolved), newest first.</summary>
    Task<List<CharacterCondition>> GetConditionsAsync(int characterId);

    /// <summary>
    /// Applies a Tilt to a character.
    /// The caller must be the character's owner or the campaign Storyteller.
    /// </summary>
    Task<CharacterTilt> ApplyTiltAsync(
        int characterId,
        TiltType type,
        string? customName,
        int? encounterId,
        string userId);

    /// <summary>
    /// Removes an active Tilt.
    /// The caller must be the character's owner or the campaign Storyteller.
    /// </summary>
    Task RemoveTiltAsync(int tiltId, string userId);

    /// <summary>Returns all active Tilts for a character.</summary>
    Task<List<CharacterTilt>> GetActiveTiltsAsync(int characterId);
}
