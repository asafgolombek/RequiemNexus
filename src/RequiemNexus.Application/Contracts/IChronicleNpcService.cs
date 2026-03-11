using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages chronicle NPCs within a campaign. All mutating operations are Storyteller-only.
/// </summary>
public interface IChronicleNpcService
{
    /// <summary>Returns all NPCs in the campaign, optionally including deceased ones.</summary>
    /// <param name="campaignId">The campaign to load NPCs for.</param>
    /// <param name="includeDeceased">When <c>true</c>, deceased NPCs are included; otherwise only living NPCs are returned.</param>
    Task<List<ChronicleNpc>> GetNpcsAsync(int campaignId, bool includeDeceased = false);

    /// <summary>Returns a single NPC with its primary faction, or <c>null</c> if not found.</summary>
    /// <param name="npcId">The NPC to load.</param>
    Task<ChronicleNpc?> GetNpcAsync(int npcId);

    /// <summary>Creates a new chronicle NPC. ST-only.</summary>
    /// <param name="campaignId">The campaign the NPC belongs to.</param>
    /// <param name="name">Display name of the NPC.</param>
    /// <param name="title">Optional honorific or title.</param>
    /// <param name="primaryFactionId">Optional faction the NPC belongs to.</param>
    /// <param name="roleInFaction">Optional description of the NPC's role.</param>
    /// <param name="publicDescription">Text visible to players.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task<ChronicleNpc> CreateNpcAsync(int campaignId, string name, string? title, int? primaryFactionId, string? roleInFaction, string publicDescription, string stUserId);

    /// <summary>Updates all editable fields on the NPC. ST-only.</summary>
    /// <param name="npcId">The NPC to update.</param>
    /// <param name="name">New display name.</param>
    /// <param name="title">New title.</param>
    /// <param name="primaryFactionId">New faction assignment.</param>
    /// <param name="roleInFaction">New role description.</param>
    /// <param name="publicDescription">New player-visible description.</param>
    /// <param name="storytellerNotes">ST-only notes.</param>
    /// <param name="linkedStatBlockId">Optional stat block link.</param>
    /// <param name="isVampire">Whether the NPC is a vampire.</param>
    /// <param name="attributesJson">Serialized attribute values.</param>
    /// <param name="skillsJson">Serialized skill values.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task UpdateNpcAsync(int npcId, string name, string? title, int? primaryFactionId, string? roleInFaction, string publicDescription, string storytellerNotes, int? linkedStatBlockId, bool isVampire, string attributesJson, string skillsJson, string stUserId);

    /// <summary>Sets the NPC's alive/deceased state. ST-only.</summary>
    /// <param name="npcId">The NPC to update.</param>
    /// <param name="isAlive">The new alive state.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task SetNpcAliveAsync(int npcId, bool isAlive, string stUserId);

    /// <summary>Deletes the NPC. ST-only.</summary>
    /// <param name="npcId">The NPC to delete.</param>
    /// <param name="stUserId">The Storyteller's user ID.</param>
    Task DeleteNpcAsync(int npcId, string stUserId);
}
