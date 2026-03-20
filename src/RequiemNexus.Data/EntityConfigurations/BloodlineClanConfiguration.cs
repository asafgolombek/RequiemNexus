using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="BloodlineClan"/>.
/// </summary>
public sealed class BloodlineClanConfiguration : IEntityTypeConfiguration<BloodlineClan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BloodlineClan> builder)
    {
        builder
            .HasOne(bc => bc.BloodlineDefinition)
            .WithMany(b => b.AllowedParentClans)
            .HasForeignKey(bc => bc.BloodlineDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(bc => bc.Clan)
            .WithMany()
            .HasForeignKey(bc => bc.ClanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(bc => bc.BloodlineDefinitionId);
        builder.HasIndex(bc => bc.ClanId);
    }
}
