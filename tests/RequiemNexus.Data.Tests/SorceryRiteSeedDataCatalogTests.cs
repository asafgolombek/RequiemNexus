using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Data.SeedData;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Ensures Crúac catalog entries load from <c>cruac_rituales.json</c> (BOM-tolerant) and match the seed file array length.
/// </summary>
public class SorceryRiteSeedDataCatalogTests
{
    [Fact]
    public void LoadCatalogEntries_CruacCount_MatchesCruacCatalogArrayLength()
    {
        using JsonDocument? doc = SeedDataLoader.TryLoadJson("cruac_rituales.json", NullLogger.Instance);
        Assert.NotNull(doc);
        int expectedCruac = doc.RootElement.GetArrayLength();

        List<SorceryRiteCatalogEntry> catalog = SorceryRiteSeedData.LoadCatalogEntries(NullLogger.Instance);
        int actualCruac = catalog.Count(e => e.SorceryType == SorceryType.Cruac);

        Assert.Equal(expectedCruac, actualCruac);
        Assert.All(catalog, e => Assert.True(e.TargetSuccesses >= 1));
        SorceryRiteCatalogEntry? pangs = catalog.FirstOrDefault(e => e.Name == "Pangs of Proserpina");
        Assert.NotNull(pangs);
        Assert.Equal(6, pangs!.TargetSuccesses);
    }

    [Fact]
    public void LoadCatalogEntries_SplitsBlandishmentAggravatedVariant()
    {
        List<SorceryRiteCatalogEntry> catalog = SorceryRiteSeedData.LoadCatalogEntries(NullLogger.Instance);
        SorceryRiteCatalogEntry? baseRite = catalog.FirstOrDefault(e => e.Name == "Blandishment of Sin");
        SorceryRiteCatalogEntry? agg = catalog.FirstOrDefault(e => e.Name == "Blandishment of Sin (Aggravated)");
        Assert.NotNull(baseRite);
        Assert.NotNull(agg);
        Assert.Equal(2, baseRite!.Rating);
        Assert.Equal(4, agg!.Rating);
        Assert.Equal(5, baseRite.TargetSuccesses);
        Assert.Equal(8, agg.TargetSuccesses);
    }
}
