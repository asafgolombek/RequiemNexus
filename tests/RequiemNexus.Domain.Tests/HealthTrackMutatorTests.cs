using RequiemNexus.Domain;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public class HealthTrackMutatorTests
{
    [Fact]
    public void ApplyDamage_fills_left_to_right_with_bashing()
    {
        string result = HealthTrackMutator.ApplyDamage(string.Empty, 4, HealthDamageKind.Bashing, 3);
        Assert.Equal("/// ", result);
    }

    [Fact]
    public void ApplyDamage_fills_when_one_box_remains()
    {
        string track = "/// ";
        string result = HealthTrackMutator.ApplyDamage(track, 4, HealthDamageKind.Bashing, 1);
        Assert.Equal("////", result);
    }

    [Fact]
    public void ApplyDamage_saturates_track_when_no_empty_after_overflow_chain()
    {
        string fullBash = "///";
        string result = HealthTrackMutator.ApplyDamage(fullBash, 3, HealthDamageKind.Bashing, 1);
        Assert.Equal("***", result);
    }

    [Fact]
    public void ApplyDamage_then_lethal_uses_empty_first()
    {
        string track = "/  ";
        string result = HealthTrackMutator.ApplyDamage(track, 3, HealthDamageKind.Lethal, 1);
        Assert.Equal("/X ", result);
    }

    [Fact]
    public void HealRightmostBashing_clears_last_bashing()
    {
        string track = "//X";
        string result = HealthTrackMutator.HealRightmostBashing(track, 3);
        Assert.Equal("/ X", result);
    }

    [Fact]
    public void CountDamagedBoxes_counts_non_space()
    {
        int n = HealthTrackMutator.CountDamagedBoxes("/ X", 3);
        Assert.Equal(2, n);
    }
}
