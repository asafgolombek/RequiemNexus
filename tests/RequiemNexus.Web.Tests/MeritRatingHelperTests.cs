using RequiemNexus.Web.Helpers;

namespace RequiemNexus.Web.Tests;

/// <summary>Regression tests for merit bullet parsing used in character creation.</summary>
public class MeritRatingHelperTests
{
    [Fact]
    public void ParseValidRatings_one_to_five_range_returns_one_through_five()
    {
        const string valid = "\u2022 to \u2022\u2022\u2022\u2022\u2022";
        List<int> ratings = MeritRatingHelper.ParseValidRatings(valid);
        Assert.Equal([1, 2, 3, 4, 5], ratings);
    }

    [Fact]
    public void IsFixedCost_one_to_five_is_false()
    {
        const string valid = "\u2022 to \u2022\u2022\u2022\u2022\u2022";
        Assert.False(MeritRatingHelper.IsFixedCost(valid));
    }

    [Fact]
    public void GetMinMax_one_to_five()
    {
        const string valid = "\u2022 to \u2022\u2022\u2022\u2022\u2022";
        Assert.Equal(1, MeritRatingHelper.GetMinRating(valid));
        Assert.Equal(5, MeritRatingHelper.GetMaxRating(valid));
    }
}
