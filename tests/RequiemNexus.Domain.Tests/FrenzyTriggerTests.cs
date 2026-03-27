using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class FrenzyTriggerTests
{
    [Fact]
    public void EnumValues_AreDistinct()
    {
        var values = Enum.GetValues<FrenzyTrigger>().Cast<int>().ToHashSet();
        Assert.Equal(4, values.Count);
    }
}
