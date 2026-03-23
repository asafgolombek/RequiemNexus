using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="BloodBond"/>.
/// </summary>
public sealed class BloodBondConfiguration : IEntityTypeConfiguration<BloodBond>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BloodBond> builder)
    {
        builder
            .HasOne(b => b.Chronicle)
            .WithMany(c => c.BloodBonds)
            .HasForeignKey(b => b.ChronicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(b => b.ThrallCharacter)
            .WithMany(c => c.BloodBondsAsThrall)
            .HasForeignKey(b => b.ThrallCharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(b => b.RegnantCharacter)
            .WithMany(c => c.BloodBondsAsRegnant)
            .HasForeignKey(b => b.RegnantCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(b => b.RegnantNpc)
            .WithMany(n => n.BloodBondsAsRegnant)
            .HasForeignKey(b => b.RegnantNpcId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.ChronicleId, b.ThrallCharacterId, b.RegnantKey }).IsUnique();
        builder.HasIndex(b => b.ChronicleId);
        builder.HasIndex(b => b.ThrallCharacterId);
    }
}
