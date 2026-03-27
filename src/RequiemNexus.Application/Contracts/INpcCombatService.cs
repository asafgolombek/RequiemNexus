using RequiemNexus.Application.DTOs;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages NPC-specific combat state (Damage, Willpower, Vitae) and NPC actions.
/// </summary>
public interface INpcCombatService
{
    /// <summary>
    /// Sets the full NPC health damage track for an initiative row (Storyteller only).
    /// The string must be exactly as many characters as <c>NpcHealthBoxes</c>, using space, '/', 'X', or '*'.
    /// </summary>
    /// <param name="initiativeEntryId">NPC initiative row id.</param>
    /// <param name="damageTrack">Full track (same convention as character health).</param>
    /// <param name="storyTellerUserId">Campaign Storyteller user id.</param>
    Task SetNpcHealthDamageAsync(int initiativeEntryId, string damageTrack, string storyTellerUserId);

    /// <summary>
    /// Applies multiple damage boxes of the same severity into the leftmost empty slots (e.g. melee resolution).
    /// </summary>
    /// <param name="entryId">NPC initiative row.</param>
    /// <param name="kind">Bashing, lethal, or aggravated (maps to track symbols).</param>
    /// <param name="instances">Number of boxes to fill; must not exceed remaining empty slots.</param>
    /// <param name="storyTellerUserId">Campaign Storyteller.</param>
    Task ApplyNpcDamageBatchAsync(int entryId, HealthDamageKind kind, int instances, string storyTellerUserId);

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
