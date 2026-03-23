using RequiemNexus.Application.Security;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>Unit tests for <see cref="CampaignInviteTokenHasher"/>.</summary>
public class CampaignInviteTokenHasherTests
{
    [Fact]
    public void Verify_ReturnsTrue_WhenTokenMatchesStoredHash()
    {
        string token = CampaignInviteTokenHasher.GenerateToken();
        string hash = CampaignInviteTokenHasher.Hash(token);
        Assert.True(CampaignInviteTokenHasher.Verify(hash, token));
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenTokenDoesNotMatch()
    {
        string hash = CampaignInviteTokenHasher.Hash("a");
        Assert.False(CampaignInviteTokenHasher.Verify(hash, "b"));
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenStoredHashNullOrMalformed()
    {
        Assert.False(CampaignInviteTokenHasher.Verify(null, "x"));
        Assert.False(CampaignInviteTokenHasher.Verify("not-hex", "x"));
    }

    [Fact]
    public void GenerateToken_ProducesDistinctValues()
    {
        string a = CampaignInviteTokenHasher.GenerateToken();
        string b = CampaignInviteTokenHasher.GenerateToken();
        Assert.NotEqual(a, b);
        Assert.InRange(a.Length, 40, 50);
    }
}
