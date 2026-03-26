using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;
using RequiemNexus.Web.Helpers;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class MeritSeedDataResourcesValidRatingsTests
{
    [Fact]
    public void LoadFromDocs_Resources_uses_real_bullets_and_parses_as_one_to_five()
    {
        List<Merit> merits = MeritSeedData.LoadFromDocs(NullLogger.Instance);
        Merit? resources = merits.FirstOrDefault(m => m.Name == "Resources");
        Assert.NotNull(resources);
        Assert.Contains("\u2022", resources!.ValidRatings, StringComparison.Ordinal);
        Assert.False(MeritRatingHelper.IsFixedCost(resources.ValidRatings));
        Assert.Equal(1, MeritRatingHelper.GetMinRating(resources.ValidRatings));
        Assert.Equal(5, MeritRatingHelper.GetMaxRating(resources.ValidRatings));
    }

    [Fact]
    public void GetAllMerits_Resources_uses_real_bullets()
    {
        List<Merit> merits = MeritSeedData.GetAllMerits();
        Merit? resources = merits.FirstOrDefault(m => m.Name == "Resources");
        Assert.NotNull(resources);
        Assert.Contains("\u2022", resources!.ValidRatings, StringComparison.Ordinal);
        Assert.False(MeritRatingHelper.IsFixedCost(resources.ValidRatings));
    }
}
