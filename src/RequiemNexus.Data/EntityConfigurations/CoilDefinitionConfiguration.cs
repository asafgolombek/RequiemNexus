using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CoilDefinition"/>.
/// </summary>
public sealed class CoilDefinitionConfiguration : IEntityTypeConfiguration<CoilDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CoilDefinition> builder)
    {
        builder
            .HasOne(c => c.Scale)
            .WithMany(s => s.Coils)
            .HasForeignKey(c => c.ScaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.ScaleId);

        builder
            .HasOne(c => c.PrerequisiteCoil)
            .WithMany()
            .HasForeignKey(c => c.PrerequisiteCoilId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.PrerequisiteCoilId);
    }
}
