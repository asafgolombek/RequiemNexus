using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="HuntingPoolDefinition"/>.
/// </summary>
public sealed class HuntingPoolDefinitionConfiguration : IEntityTypeConfiguration<HuntingPoolDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HuntingPoolDefinition> builder)
    {
        builder.HasIndex(h => h.PredatorType).IsUnique();
    }
}
