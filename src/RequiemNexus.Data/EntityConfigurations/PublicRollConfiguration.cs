using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="PublicRoll"/>.
/// </summary>
public sealed class PublicRollConfiguration : IEntityTypeConfiguration<PublicRoll>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PublicRoll> builder)
    {
        builder.HasIndex(r => r.Slug).IsUnique();

        builder
            .HasOne(r => r.RolledByUser)
            .WithMany()
            .HasForeignKey(r => r.RolledByUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
