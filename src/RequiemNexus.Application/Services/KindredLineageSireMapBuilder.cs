using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Builds in-chronicle PC sire adjacency maps for Blood Sympathy and ritual modifiers.
/// </summary>
public static class KindredLineageSireMapBuilder
{
    /// <summary>
    /// Loads each character's sire link when the sire is also a PC in the same chronicle.
    /// </summary>
    /// <param name="db">The EF Core context.</param>
    /// <param name="campaignId">The chronicle identifier.</param>
    /// <returns>Character id → optional sire character id.</returns>
    public static async Task<IReadOnlyDictionary<int, int?>> BuildForCampaignAsync(
        ApplicationDbContext db,
        int campaignId)
    {
        var rows = await db.Characters
            .AsNoTracking()
            .Where(c => c.CampaignId == campaignId)
            .Select(c => new { c.Id, c.SireCharacterId })
            .ToListAsync();

        HashSet<int> idSet = rows.Select(r => r.Id).ToHashSet();
        return rows.ToDictionary(
            r => r.Id,
            r => r.SireCharacterId.HasValue && idSet.Contains(r.SireCharacterId.Value)
                ? r.SireCharacterId
                : null);
    }
}
