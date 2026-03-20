using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="MeritPrerequisite"/>.
/// </summary>
public sealed class MeritPrerequisiteConfiguration : IEntityTypeConfiguration<MeritPrerequisite>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MeritPrerequisite> builder)
    {
        builder
            .HasOne(p => p.Merit)
            .WithMany(m => m.Prerequisites)
            .HasForeignKey(p => p.MeritId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.MeritId);
    }
}
