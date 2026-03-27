using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Tests for <see cref="NpcHealthDamageTrack"/>.
/// </summary>
public class NpcHealthDamageTrackTests
{
    [Fact]
    public void Normalize_ShortValue_PadsRightWithSpaces()
    {
        string result = NpcHealthDamageTrack.Normalize("///", 7);
        Assert.Equal(7, result.Length);
        Assert.Equal("///    ", result);
    }

    [Fact]
    public void Normalize_LongValue_TrimsToBoxCount()
    {
        string result = NpcHealthDamageTrack.Normalize("///XXXX", 4);
        Assert.Equal("///X", result);
    }

    [Fact]
    public void ValidateFullTrack_InvalidLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => NpcHealthDamageTrack.ValidateFullTrack("   ", 5));
    }

    [Fact]
    public void ValidateFullTrack_InvalidChar_Throws()
    {
        Assert.Throws<ArgumentException>(() => NpcHealthDamageTrack.ValidateFullTrack("a    ", 5));
    }
}
