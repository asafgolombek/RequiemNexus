using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class KindredLineageDegreeTests
{
    [Fact]
    public void TryGetShortestDegree_Self_ReturnsZero()
    {
        var map = new Dictionary<int, int?> { [1] = null, [2] = 1 };
        Assert.Equal(0, KindredLineageDegree.TryGetShortestDegree(1, 1, map));
    }

    [Fact]
    public void TryGetShortestDegree_DirectSire_ReturnsOne()
    {
        var map = new Dictionary<int, int?> { [1] = null, [2] = 1 };
        Assert.Equal(1, KindredLineageDegree.TryGetShortestDegree(2, 1, map));
    }

    [Fact]
    public void TryGetShortestDegree_Grandchild_ReturnsTwo()
    {
        var map = new Dictionary<int, int?> { [1] = null, [2] = 1, [3] = 2 };
        Assert.Equal(2, KindredLineageDegree.TryGetShortestDegree(3, 1, map));
    }

    [Fact]
    public void TryGetShortestDegree_MissingId_ReturnsNull()
    {
        var map = new Dictionary<int, int?> { [1] = null };
        Assert.Null(KindredLineageDegree.TryGetShortestDegree(1, 2, map));
    }
}
