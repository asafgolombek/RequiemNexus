using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages active combat encounters and initiative tracking.
/// </summary>
public interface IEncounterService
{
    /// <summary>
    /// Activates a draft encounter, rolls initiative for all NPC templates, and enforces a single active fight per campaign.
    /// </summary>
    Task LaunchEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Marks the next unacted participant as having acted. Wraps rounds and increments <see cref="CombatEncounter.CurrentRound"/>.
    /// </summary>
    Task AdvanceTurnAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Closes the encounter.
    /// </summary>
    Task ResolveEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Pauses a running encounter: initiative remains in the database; live session initiative is cleared.
    /// </summary>
    Task PauseEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Resumes a paused encounter and republishes initiative to the session.
    /// </summary>
    Task ResumeEncounterAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Persists manual initiative reordering from the Storyteller UI.
    /// </summary>
    Task ReorderInitiativeAsync(int encounterId, IReadOnlyList<int> entryIdsInOrder, string storyTellerUserId);

    /// <summary>
    /// Places the current actor on hold and advances to the next combatant.
    /// </summary>
    Task HoldActionAsync(int encounterId, string storyTellerUserId);

    /// <summary>
    /// Releases a held combatant so they act immediately (ST-chosen entry).
    /// </summary>
    Task ReleaseHeldActionAsync(int encounterId, int entryId, string storyTellerUserId);
}
