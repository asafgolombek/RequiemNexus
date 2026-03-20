using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

public sealed class RiteRequirementValidatorTests
{
    [Fact]
    public void ParseRequirements_null_yields_empty()
    {
        var r = RiteRequirementValidator.ParseRequirements(null);
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Value!);
    }

    [Fact]
    public void ParseRequirements_valid_array()
    {
        const string json = """[{"type":"InternalVitae","value":2,"isConsumed":true}]""";
        var r = RiteRequirementValidator.ParseRequirements(json);
        Assert.True(r.IsSuccess);
        Assert.Single(r.Value!);
        Assert.Equal(SacrificeType.InternalVitae, r.Value![0].Type);
        Assert.Equal(2, r.Value[0].Value);
    }

    [Fact]
    public void ValidateResources_insufficient_vitae_fails()
    {
        var req = new List<RiteRequirement> { new(SacrificeType.InternalVitae, 3) };
        var snap = new RiteActivationResourceSnapshot(2, 5, 0);
        var r = RiteRequirementValidator.ValidateResources(req, snap);
        Assert.False(r.IsSuccess);
        Assert.Contains("Vitae", r.Error!, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateAcknowledgments_heart_unacknowledged_fails()
    {
        var req = new List<RiteRequirement> { new(SacrificeType.Heart, 1) };
        var ack = new RiteActivationAcknowledgment();
        var r = RiteRequirementValidator.ValidateAcknowledgments(req, ack);
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public void ValidateAcknowledgments_heart_acknowledged_succeeds()
    {
        var req = new List<RiteRequirement> { new(SacrificeType.Heart, 1) };
        var ack = new RiteActivationAcknowledgment(AcknowledgeHeart: true);
        var r = RiteRequirementValidator.ValidateAcknowledgments(req, ack);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void AggregateInternalCosts_sums_vitae_types()
    {
        var req = new List<RiteRequirement>
        {
            new(SacrificeType.InternalVitae, 1),
            new(SacrificeType.SpilledVitae, 2),
        };
        var (v, w, s) = RiteRequirementValidator.AggregateInternalCosts(req);
        Assert.Equal(3, v);
        Assert.Equal(0, w);
        Assert.Equal(0, s);
    }
}
