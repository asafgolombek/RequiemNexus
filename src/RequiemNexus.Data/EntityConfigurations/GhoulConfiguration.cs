using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="Ghoul"/>.
/// </summary>
public sealed class GhoulConfiguration : IEntityTypeConfiguration<Ghoul>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Ghoul> builder)
    {
        builder
            .HasOne(g => g.Chronicle)
            .WithMany(c => c.Ghouls)
            .HasForeignKey(g => g.ChronicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(g => g.RegnantCharacter)
            .WithMany(c => c.GhoulsAsRegnant)
            .HasForeignKey(g => g.RegnantCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(g => g.RegnantNpc)
            .WithMany(n => n.GhoulsAsRegnant)
            .HasForeignKey(g => g.RegnantNpcId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(g => g.ChronicleId);
        builder.HasIndex(g => g.RegnantCharacterId);
        builder.HasIndex(g => g.RegnantNpcId);
    }
}
