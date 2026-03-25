using RequiemNexus.Domain;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class WoundPenaltyResolverTests
{
    [Fact]
    public void GetWoundPenaltyDice_returns_zero_when_last_three_boxes_clear()
    {
        string track = "XXX    "; // 7 boxes, only first three damaged
        int pen = WoundPenaltyResolver.GetWoundPenaltyDice(track, 7);
        Assert.Equal(0, pen);
    }

    [Fact]
    public void GetWoundPenaltyDice_minus_one_when_third_from_end_damaged()
    {
        // max=7 → penalty −1 when box index max−3 (4) is damaged.
        string track = "XXXXX  ";
        int pen = WoundPenaltyResolver.GetWoundPenaltyDice(track, 7);
        Assert.Equal(-1, pen);
    }

    [Fact]
    public void GetWoundPenaltyDice_minus_three_when_last_box_damaged()
    {
        string track = "      X";
        int pen = WoundPenaltyResolver.GetWoundPenaltyDice(track, 7);
        Assert.Equal(-3, pen);
    }

    [Fact]
    public void IsIncapacitated_true_when_all_boxes_marked()
    {
        string track = "XXXXXXX";
        Assert.True(WoundPenaltyResolver.IsIncapacitated(track, 7));
    }

    [Fact]
    public void IsIncapacitated_false_with_any_empty_box()
    {
        string track = "XXXXXX ";
        Assert.False(WoundPenaltyResolver.IsIncapacitated(track, 7));
    }
}
