using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Shared EF include graphs for character detail loads so edit and read paths stay aligned.
/// </summary>
public static class CharacterQueryableExtensions
{
    /// <summary>
    /// Full navigation graph for the owner edit surface (tracked load in <see cref="CharacterManagementService"/>).
    /// </summary>
    public static IQueryable<Character> IncludeCharacterDetailEditGraph(this IQueryable<Character> query)
    {
        return query
            .Include(c => c.Clan)!.ThenInclude(cl => cl!.ClanDisciplines)
            .Include(c => c.Covenant)
            .Include(c => c.Campaign)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline!).ThenInclude(d => d.Powers)
            .Include(c => c.Bloodlines).ThenInclude(b => b.BloodlineDefinition)
            .Include(c => c.Devotions).ThenInclude(d => d.DevotionDefinition)
            .Include(c => c.Rites).ThenInclude(r => r.SorceryRiteDefinition)
            .Include(c => c.Coils).ThenInclude(cc => cc.CoilDefinition).ThenInclude(c => c!.Scale)
            .Include(c => c.ChosenMysteryScale)
            .Include(c => c.PendingChosenMysteryScale)
            .Include(c => c.Banes)
            .Include(c => c.Aspirations)
            .Include(c => c.CharacterAssets).ThenInclude(ca => ca.Asset);
    }

    /// <summary>
    /// Reduced graph for shared / read-only character views (access-checked snapshot).
    /// </summary>
    public static IQueryable<Character> IncludeCharacterAccessSnapshotGraph(this IQueryable<Character> query)
    {
        return query
            .Include(c => c.Clan)!.ThenInclude(cl => cl!.ClanDisciplines)
            .Include(c => c.Campaign)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline!).ThenInclude(d => d.Powers)
            .Include(c => c.Bloodlines).ThenInclude(b => b.BloodlineDefinition)
            .Include(c => c.CharacterAssets).ThenInclude(ca => ca.Asset)
            .Include(c => c.Aspirations)
            .Include(c => c.Banes);
    }
}
