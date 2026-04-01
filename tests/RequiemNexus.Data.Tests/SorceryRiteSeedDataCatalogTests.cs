using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Data.SeedData;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Ensures Crúac catalog entries load from <c>rites.json</c> (BOM-tolerant) and match the seed file array length.
/// </summary>
public class SorceryRiteSeedDataCatalogTests
{
    [Fact]
    public void LoadCatalogEntries_CruacCount_MatchesRitesJsonArrayLength()
    {
        using JsonDocument? doc = SeedDataLoader.TryLoadJson("rites.json", NullLogger.Instance);
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
}
