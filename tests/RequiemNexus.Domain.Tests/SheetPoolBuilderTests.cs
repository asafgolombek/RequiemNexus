using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Tests for <see cref="SheetPoolBuilder"/> label parsing used by the dice UI.
/// </summary>
public class SheetPoolBuilderTests
{
    [Fact]
    public void TryTraitFromLabel_AttributeDisplayName_ReturnsAttributeReference()
    {
        TraitReference? r = SheetPoolBuilder.TryTraitFromLabel("Strength");
        Assert.NotNull(r);
        Assert.Equal(TraitType.Attribute, r!.Type);
        Assert.Equal(AttributeId.Strength, r.AttributeId);
    }

    [Fact]
    public void TryTraitFromLabel_SkillWithSpaces_ReturnsSkillReference()
    {
        TraitReference? r = SheetPoolBuilder.TryTraitFromLabel("Animal Ken");
        Assert.NotNull(r);
        Assert.Equal(TraitType.Skill, r!.Type);
        Assert.Equal(SkillId.AnimalKen, r.SkillId);
    }

    [Fact]
    public void TryCreate_PrimaryAndAssociated_BuildsTwoTraitPool()
    {
        PoolDefinition? pool = SheetPoolBuilder.TryCreate("Wits", "Investigation");
        Assert.NotNull(pool);
        Assert.Equal(2, pool!.Traits.Count);
    }

    [Fact]
    public void TryCreate_UnknownPrimary_ReturnsNull()
    {
        PoolDefinition? pool = SheetPoolBuilder.TryCreate("NotATrait", null);
        Assert.Null(pool);
    }
}
