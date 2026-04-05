using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Seeds the equipment/asset catalog and deferred <see cref="AssetCapability"/> rows.
/// </summary>
public sealed class EquipmentSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 50;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        await SeedEquipmentCatalogAsync(context);
    }

    private static async Task SeedEquipmentCatalogAsync(ApplicationDbContext context)
    {
        IReadOnlyList<Asset> catalog = AssetSeedData.LoadCatalogAssets();
        if (catalog.Count == 0)
        {
            return;
        }

        HashSet<string> existing = (await context.Assets
                .Where(a => a.Slug != null)
                .Select(a => a.Slug!)
                .ToListAsync())
            .ToHashSet(StringComparer.Ordinal);

        List<Asset> toAdd = catalog
            .Where(a => a.Slug != null && !existing.Contains(a.Slug))
            .ToList();
        if (toAdd.Count == 0)
        {
            await SeedDeferredAssetCapabilitiesAsync(context);
            return;
        }

        await context.Assets.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
        await SeedDeferredAssetCapabilitiesAsync(context);
    }

    private static async Task SeedDeferredAssetCapabilitiesAsync(ApplicationDbContext context)
    {
        IReadOnlyList<DeferredAssetCapability> deferred = AssetSeedData.LoadDeferredCapabilities();
        if (deferred.Count == 0)
        {
            return;
        }

        Dictionary<string, int> idBySlug = await context.Assets
            .Where(a => a.Slug != null)
            .Select(a => new { a.Slug, a.Id })
            .ToDictionaryAsync(x => x.Slug!, x => x.Id, StringComparer.Ordinal);

        HashSet<(int AssetId, AssetCapabilityKind Kind)> existingCapabilities = (
            await context.AssetCapabilities
                .Select(c => new { c.AssetId, c.Kind })
                .ToListAsync())
            .Select(x => (x.AssetId, x.Kind))
            .ToHashSet();

        foreach (DeferredAssetCapability d in deferred)
        {
            if (!idBySlug.TryGetValue(d.OwnerAssetSlug, out int ownerId))
            {
                continue;
            }

            if (existingCapabilities.Contains((ownerId, d.Kind)))
            {
                continue;
            }

            int? profileId = null;
            if (d.WeaponProfileSlug != null && idBySlug.TryGetValue(d.WeaponProfileSlug, out int pid))
            {
                profileId = pid;
            }

            context.AssetCapabilities.Add(new AssetCapability
            {
                AssetId = ownerId,
                Kind = d.Kind,
                AssistsSkillName = d.AssistsSkillName,
                DiceBonusMin = d.DiceBonusMin,
                DiceBonusMax = d.DiceBonusMax,
                WeaponProfileAssetId = profileId,
            });
            existingCapabilities.Add((ownerId, d.Kind));
        }

        await context.SaveChangesAsync();
    }
}
