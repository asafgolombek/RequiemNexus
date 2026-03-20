using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="DevotionPrerequisite"/>.
/// </summary>
public sealed class DevotionPrerequisiteConfiguration : IEntityTypeConfiguration<DevotionPrerequisite>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DevotionPrerequisite> builder)
    {
        builder
            .HasOne(p => p.DevotionDefinition)
            .WithMany(d => d.Prerequisites)
            .HasForeignKey(p => p.DevotionDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(p => p.Discipline)
            .WithMany()
            .HasForeignKey(p => p.DisciplineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.DevotionDefinitionId);
        builder.HasIndex(p => p.DisciplineId);
    }
}
