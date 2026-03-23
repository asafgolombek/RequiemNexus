using RequiemNexus.Web.Helpers;

namespace RequiemNexus.Web.Tests;

public class MeritValidRatingsDisplayFormatterTests
{
    [Fact]
    public void FormatLabel_range_uses_en_dash()
    {
        const string valid = "\u2022 to \u2022\u2022\u2022\u2022\u2022";
        string label = MeritValidRatingsDisplayFormatter.FormatLabel(valid);
        Assert.Equal("1\u20135", label);
    }

    [Fact]
    public void FormatLabel_single_dot()
    {
        const string valid = "\u2022";
        Assert.Equal("1", MeritValidRatingsDisplayFormatter.FormatLabel(valid));
    }
}
