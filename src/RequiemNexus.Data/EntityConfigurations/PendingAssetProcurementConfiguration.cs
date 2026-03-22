using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="PendingAssetProcurement"/>.
/// </summary>
public sealed class PendingAssetProcurementConfiguration : IEntityTypeConfiguration<PendingAssetProcurement>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PendingAssetProcurement> builder)
    {
        builder.HasIndex(p => new { p.CharacterId, p.Status });

        builder
            .HasOne(p => p.Character)
            .WithMany()
            .HasForeignKey(p => p.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(p => p.Asset)
            .WithMany()
            .HasForeignKey(p => p.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
