using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Unit tests for VtR 2e Social maneuvering math.
/// </summary>
public class SocialManeuveringEngineTests
{
    [Theory]
    [InlineData(3, 2, false, false, false, 2)]
    [InlineData(3, 2, true, false, false, 4)]
    [InlineData(3, 2, false, true, false, 3)]
    [InlineData(3, 2, false, false, true, 3)]
    [InlineData(3, 2, true, true, true, 6)]
    public void ComputeInitialDoorCount_AppliesBaseAndModifiers(
        int resolve,
        int composure,
        bool bp,
        bool aspiration,
        bool virtue,
        int expected)
    {
        int doors = SocialManeuveringEngine.ComputeInitialDoorCount(resolve, composure, bp, aspiration, virtue);
        Assert.Equal(expected, doors);
    }

    [Fact]
    public void ComputeInitialDoorCount_ClampsAttributesAndFloorsAtOne()
    {
        int doors = SocialManeuveringEngine.ComputeInitialDoorCount(0, 10, false, false, false);
        Assert.Equal(1, doors);
    }

    [Fact]
    public void GetMinimumIntervalBetweenOpenDoorRolls_Hostile_IsNull()
    {
        Assert.Null(SocialManeuveringEngine.GetMinimumIntervalBetweenOpenDoorRolls(ImpressionLevel.Hostile));
    }

    [Fact]
    public void ValidateOpenDoorRollTiming_FirstRoll_Allows()
    {
        var result = SocialManeuveringEngine.ValidateOpenDoorRollTiming(
            lastRollAtUtc: null,
            ImpressionLevel.Good,
            nowUtc: DateTimeOffset.UtcNow);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void ValidateOpenDoorRollTiming_Hostile_Blocks()
    {
        var result = SocialManeuveringEngine.ValidateOpenDoorRollTiming(
            null,
            ImpressionLevel.Hostile,
            DateTimeOffset.UtcNow);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ValidateOpenDoorRollTiming_BeforeInterval_Blocks()
    {
        DateTimeOffset last = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset now = last.AddDays(3);

        var result = SocialManeuveringEngine.ValidateOpenDoorRollTiming(
            last,
            ImpressionLevel.Average,
            now);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ValidateOpenDoorRollTiming_AfterInterval_Allows()
    {
        DateTimeOffset last = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset now = last.AddDays(8);

        var result = SocialManeuveringEngine.ValidateOpenDoorRollTiming(
            last,
            ImpressionLevel.Average,
            now);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void GetDoorsOpenedByOpenDoorRoll_Exceptional_OpensTwo()
    {
        int doors = SocialManeuveringEngine.GetDoorsOpenedByOpenDoorRoll(5, true, false);
        Assert.Equal(2, doors);
    }

    [Fact]
    public void GetDoorsOpenedByOpenDoorRoll_Success_OpensOne()
    {
        int doors = SocialManeuveringEngine.GetDoorsOpenedByOpenDoorRoll(2, false, false);
        Assert.Equal(1, doors);
    }

    [Fact]
    public void GetDoorsOpenedByOpenDoorRoll_Failure_OpensZero()
    {
        int doors = SocialManeuveringEngine.GetDoorsOpenedByOpenDoorRoll(0, false, false);
        Assert.Equal(0, doors);
    }

    [Fact]
    public void GetDoorsOpenedByOpenDoorRoll_Dramatic_OpensZero()
    {
        int doors = SocialManeuveringEngine.GetDoorsOpenedByOpenDoorRoll(0, false, true);
        Assert.Equal(0, doors);
    }

    [Fact]
    public void ComputeHardLeverageDoorsRemoved_SmallGap_RemovesOne()
    {
        var r = SocialManeuveringEngine.ComputeHardLeverageDoorsRemoved(breakingPointSeverity: 7, persuaderHumanity: 7);
        Assert.True(r.IsSuccess);
        Assert.Equal(1, r.Value);
    }

    [Fact]
    public void ComputeHardLeverageDoorsRemoved_LargeGap_RemovesTwo()
    {
        var r = SocialManeuveringEngine.ComputeHardLeverageDoorsRemoved(breakingPointSeverity: 10, persuaderHumanity: 5);
        Assert.True(r.IsSuccess);
        Assert.Equal(2, r.Value);
    }

    [Theory]
    [InlineData(0, 5, 3, 1, 2)]
    [InlineData(2, 2, 3, 1, 1)]
    [InlineData(0, 2, 5, 0, 2)]
    public void AccrueInvestigationTowardClues_ComputesProgressAndClueCount(
        int progress,
        int add,
        int threshold,
        int expectedClues,
        int expectedNewProgress)
    {
        (int newProgress, int clues) = SocialManeuveringEngine.AccrueInvestigationTowardClues(progress, add, threshold);
        Assert.Equal(expectedClues, clues);
        Assert.Equal(expectedNewProgress, newProgress);
    }

    [Fact]
    public void AccrueInvestigationTowardClues_ZeroAdds_ReturnsUnchanged()
    {
        (int newProgress, int clues) = SocialManeuveringEngine.AccrueInvestigationTowardClues(2, 0, 3);
        Assert.Equal(0, clues);
        Assert.Equal(2, newProgress);
    }

    [Fact]
    public void ShouldFailFromHostileWeek_AfterSevenDays_True()
    {
        DateTimeOffset hostileSince = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset now = hostileSince.AddDays(7);
        Assert.True(SocialManeuveringEngine.ShouldFailFromHostileWeek(hostileSince, ImpressionLevel.Hostile, now));
    }

    [Fact]
    public void ShouldFailFromHostileWeek_BeforeSevenDays_False()
    {
        DateTimeOffset hostileSince = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset now = hostileSince.AddDays(6);
        Assert.False(SocialManeuveringEngine.ShouldFailFromHostileWeek(hostileSince, ImpressionLevel.Hostile, now));
    }

    [Fact]
    public void ComputeMaximumSocialApproachDicePool_ReturnsLargestListedApproach()
    {
        int max = SocialManeuveringEngine.ComputeMaximumSocialApproachDicePool(
            intelligence: 2,
            wits: 2,
            manipulation: 5,
            presence: 2,
            empathy: 0,
            expression: 0,
            intimidation: 0,
            persuasion: 5,
            socialize: 0,
            streetwise: 0,
            subterfuge: 0);

        Assert.Equal(10, max);
    }

    [Theory]
    [InlineData(3, 2, 1)]
    [InlineData(3, 5, 0)]
    [InlineData(0, 0, 0)]
    public void ApplyInterceptorReductionToSuccesses_FloorsAtZero(int initiator, int interceptors, int expected)
    {
        Assert.Equal(
            expected,
            SocialManeuveringEngine.ApplyInterceptorReductionToSuccesses(initiator, interceptors));
    }

    [Fact]
    public void GetDoorsOpenedByOpenDoorRoll_AfterInterceptorReductionToZero_OpensNoDoors()
    {
        int adjusted = SocialManeuveringEngine.ApplyInterceptorReductionToSuccesses(2, 4);
        Assert.Equal(0, adjusted);
        int doors = SocialManeuveringEngine.GetDoorsOpenedByOpenDoorRoll(adjusted, isExceptionalSuccess: false, isDramaticFailure: false);
        Assert.Equal(0, doors);
    }
}
