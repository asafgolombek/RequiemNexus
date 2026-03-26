using RequiemNexus.Domain;
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
}
