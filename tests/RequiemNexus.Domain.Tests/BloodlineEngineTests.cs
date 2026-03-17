using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Unit tests for BloodlineEngine prerequisite validation.
/// </summary>
public class BloodlineEngineTests
{
    [Fact]
    public void ValidateJoinPrerequisites_NoClan_ReturnsFailure()
    {
        var result = BloodlineEngine.ValidateJoinPrerequisites(
            characterClanId: null,
            characterBloodPotency: 3,
            allowedParentClanIds: [1],
            prerequisiteBloodPotency: 2);

        Assert.False(result.IsSuccess);
        Assert.Contains("clan", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateJoinPrerequisites_BloodPotencyBelowRequired_ReturnsFailure()
    {
        var result = BloodlineEngine.ValidateJoinPrerequisites(
            characterClanId: 1,
            characterBloodPotency: 1,
            allowedParentClanIds: [1],
            prerequisiteBloodPotency: 2);

        Assert.False(result.IsSuccess);
        Assert.Contains("Blood Potency", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateJoinPrerequisites_ClanNotAllowed_ReturnsFailure()
    {
        var result = BloodlineEngine.ValidateJoinPrerequisites(
            characterClanId: 1,
            characterBloodPotency: 3,
            allowedParentClanIds: [2, 3],
            prerequisiteBloodPotency: 2);

        Assert.False(result.IsSuccess);
        Assert.Contains("allowed parent", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateJoinPrerequisites_EmptyAllowedClans_ReturnsFailure()
    {
        var result = BloodlineEngine.ValidateJoinPrerequisites(
            characterClanId: 1,
            characterBloodPotency: 3,
            allowedParentClanIds: [],
            prerequisiteBloodPotency: 2);

        Assert.False(result.IsSuccess);
        Assert.Contains("parent clans", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateJoinPrerequisites_Valid_ReturnsSuccess()
    {
        var result = BloodlineEngine.ValidateJoinPrerequisites(
            characterClanId: 1,
            characterBloodPotency: 3,
            allowedParentClanIds: [1, 2],
            prerequisiteBloodPotency: 2);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public void ValidateJoinPrerequisites_BP2Exactly_ReturnsSuccess()
    {
        var result = BloodlineEngine.ValidateJoinPrerequisites(
            characterClanId: 1,
            characterBloodPotency: 2,
            allowedParentClanIds: [1],
            prerequisiteBloodPotency: 2);

        Assert.True(result.IsSuccess);
    }
}
