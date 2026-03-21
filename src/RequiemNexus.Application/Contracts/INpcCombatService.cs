using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages NPC-specific combat state (Damage, Willpower, Vitae) and NPC actions.
/// </summary>
public interface INpcCombatService
{
    /// <summary>
    /// Appends one damage box to an NPC initiative entry.
    /// </summary>
    Task ApplyNpcDamageAsync(int entryId, char damageType, string storyTellerUserId);

    /// <summary>
    /// Removes the last damage mark from an NPC track.
    /// </summary>
    Task HealNpcDamageAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Spends one willpower from an NPC initiative row (ST only).
    /// </summary>
    Task SpendNpcWillpowerAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Restores one willpower to an NPC initiative row (ST only).
    /// </summary>
    Task RestoreNpcWillpowerAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Spends one vitae from an NPC initiative row (ST only, Kindred rows only).
    /// </summary>
    Task SpendNpcVitaeAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Restores one vitae to an NPC initiative row (ST only).
    /// </summary>
    Task RestoreNpcVitaeAsync(int entryId, string storyTellerUserId);

    /// <summary>
    /// Sets whether players can see the NPC's true name.
    /// </summary>
    Task SetNpcEntryRevealAsync(int entryId, bool revealed, string? maskedDisplayName, string storyTellerUserId);

    /// <summary>
    /// Rolls a dice pool for an NPC initiative row (Storyteller only).
    /// </summary>
    Task<NpcEncounterRollResultDto> RollNpcEncounterPoolAsync(
        int initiativeEntryId,
        string? trait1,
        string? trait2,
        int? manualDicePool,
        string storyTellerUserId);
}
