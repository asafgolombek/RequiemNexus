using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="ScaleDefinition"/>.
/// </summary>
public sealed class ScaleDefinitionConfiguration : IEntityTypeConfiguration<ScaleDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ScaleDefinition> builder)
    {
        builder.HasIndex(s => s.Name).IsUnique();
    }
}
