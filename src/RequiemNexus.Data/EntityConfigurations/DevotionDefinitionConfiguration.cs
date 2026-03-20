using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="DevotionDefinition"/>.
/// </summary>
public sealed class DevotionDefinitionConfiguration : IEntityTypeConfiguration<DevotionDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DevotionDefinition> builder)
    {
        builder
            .HasOne(d => d.RequiredBloodline)
            .WithMany()
            .HasForeignKey(d => d.RequiredBloodlineId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(d => d.RequiredBloodlineId);
    }
}
