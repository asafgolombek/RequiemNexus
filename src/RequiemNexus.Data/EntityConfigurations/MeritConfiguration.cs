using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="Merit"/>.
/// </summary>
public sealed class MeritConfiguration : IEntityTypeConfiguration<Merit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Merit> builder)
    {
        builder.HasIndex(m => m.Name).IsUnique();
    }
}
