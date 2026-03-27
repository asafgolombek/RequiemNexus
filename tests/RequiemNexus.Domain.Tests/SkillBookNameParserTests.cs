using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Tests for <see cref="SkillBookNameParser"/>.
/// </summary>
public class SkillBookNameParserTests
{
    [Theory]
    [InlineData("Larceny", SkillId.Larceny)]
    [InlineData("larceny", SkillId.Larceny)]
    [InlineData("Animal Ken", SkillId.AnimalKen)]
    [InlineData("Streetwise", SkillId.Streetwise)]
    public void TryParseBookName_KnownLabels_ReturnsTrue(string label, SkillId expected)
    {
        bool ok = SkillBookNameParser.TryParseBookName(label, out SkillId id);
        Assert.True(ok);
        Assert.Equal(expected, id);
    }

    [Fact]
    public void TryParseBookName_Unknown_ReturnsFalse()
    {
        bool ok = SkillBookNameParser.TryParseBookName("Teamwork", out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryParseBookName_NullOrEmpty_ReturnsFalse()
    {
        Assert.False(SkillBookNameParser.TryParseBookName(null, out _));
        Assert.False(SkillBookNameParser.TryParseBookName("   ", out _));
    }
}
