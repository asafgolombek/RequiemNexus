using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application service for managing NPC stat blocks — pre-built canonical blocks and
/// campaign-specific custom blocks created by the Storyteller.
/// </summary>
public interface INpcStatBlockService
{
    /// <summary>Returns all pre-built (canonical) stat blocks visible to any campaign.</summary>
    Task<List<NpcStatBlock>> GetPrebuiltBlocksAsync();

    /// <summary>Returns all custom stat blocks belonging to the given campaign.</summary>
    Task<List<NpcStatBlock>> GetCampaignBlocksAsync(int campaignId);

    /// <summary>
    /// Returns the combined list of pre-built blocks and campaign-specific custom blocks,
    /// suitable for populating a picker in the encounter manager.
    /// </summary>
    Task<List<NpcStatBlock>> GetAvailableBlocksAsync(int campaignId);

    /// <summary>Returns a single stat block by its primary key, or null if not found.</summary>
    Task<NpcStatBlock?> GetBlockAsync(int statBlockId);

    /// <summary>
    /// Creates a new custom stat block scoped to the given campaign.
    /// Only the campaign Storyteller may create blocks.
    /// </summary>
    Task<NpcStatBlock> CreateBlockAsync(
        int campaignId,
        string name,
        string concept,
        int size,
        int health,
        int willpower,
        int bludgeoningArmor,
        int lethalArmor,
        string attributesJson,
        string skillsJson,
        string disciplinesJson,
        string notes,
        string stUserId);

    /// <summary>
    /// Updates an existing custom stat block.
    /// Only the campaign Storyteller may update blocks.
    /// Throws <see cref="UnauthorizedAccessException"/> when called on a pre-built block.
    /// </summary>
    Task UpdateBlockAsync(
        int statBlockId,
        string name,
        string concept,
        int size,
        int health,
        int willpower,
        int bludgeoningArmor,
        int lethalArmor,
        string attributesJson,
        string skillsJson,
        string disciplinesJson,
        string notes,
        string stUserId);

    /// <summary>
    /// Deletes a custom stat block.
    /// Throws <see cref="UnauthorizedAccessException"/> when called on a pre-built block or by a non-ST.
    /// </summary>
    Task DeleteBlockAsync(int statBlockId, string stUserId);
}
