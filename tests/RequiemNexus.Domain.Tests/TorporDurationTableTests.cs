using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class TorporDurationTableTests
{
    [Fact]
    public void MinimumDaysById_ContainsBloodPotencyOneThroughTen()
    {
        for (int bp = 1; bp <= 10; bp++)
        {
            Assert.True(TorporDurationTable.MinimumDaysById.ContainsKey(bp));
        }
    }

    [Fact]
    public void MinimumDaysById_ValuesAreStrictlyAscending_ForBpOneThroughNine()
    {
        int previous = 0;
        for (int bp = 1; bp <= 9; bp++)
        {
            int current = TorporDurationTable.MinimumDaysById[bp];
            Assert.True(current > previous, $"BP {bp} should exceed prior tier.");
            previous = current;
        }
    }

    [Fact]
    public void MinimumDaysById_BloodPotencyTen_IsIntMaxValue()
    {
        Assert.Equal(int.MaxValue, TorporDurationTable.MinimumDaysById[10]);
    }

    [Fact]
    public void GetMinimumDays_OutOfRange_FallsBackToBpOne()
    {
        Assert.Equal(TorporDurationTable.MinimumDaysById[1], TorporDurationTable.GetMinimumDays(0));
        Assert.Equal(TorporDurationTable.MinimumDaysById[1], TorporDurationTable.GetMinimumDays(99));
    }

    [Fact]
    public void GetEffectiveBloodPotencyForTorporDuration_AddsNecromancy_CappedAtTen()
    {
        Assert.Equal(3, TorporDurationTable.GetEffectiveBloodPotencyForTorporDuration(1, 2));
        Assert.Equal(10, TorporDurationTable.GetEffectiveBloodPotencyForTorporDuration(8, 5));
    }

    [Fact]
    public void GetEffectiveBloodPotencyForTorporDuration_NegativeNecromancy_TreatedAsZero()
    {
        Assert.Equal(2, TorporDurationTable.GetEffectiveBloodPotencyForTorporDuration(2, -1));
    }
}
