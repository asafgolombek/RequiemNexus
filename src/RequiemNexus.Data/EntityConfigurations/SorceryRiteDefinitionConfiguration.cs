using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="SorceryRiteDefinition"/>.
/// </summary>
public sealed class SorceryRiteDefinitionConfiguration : IEntityTypeConfiguration<SorceryRiteDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SorceryRiteDefinition> builder)
    {
        builder
            .HasOne(s => s.RequiredCovenant)
            .WithMany()
            .HasForeignKey(s => s.RequiredCovenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.RequiredCovenantId);

        builder
            .HasOne(s => s.RequiredClan)
            .WithMany()
            .HasForeignKey(s => s.RequiredClanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.RequiredClanId);
    }
}
