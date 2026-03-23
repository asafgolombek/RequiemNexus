using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="AssetCapability"/>.
/// </summary>
public sealed class AssetCapabilityConfiguration : IEntityTypeConfiguration<AssetCapability>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AssetCapability> builder)
    {
        builder.HasIndex(ac => ac.AssetId);

        builder
            .HasOne(ac => ac.Asset)
            .WithMany(a => a.Capabilities)
            .HasForeignKey(ac => ac.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ac => ac.WeaponProfileAsset)
            .WithMany()
            .HasForeignKey(ac => ac.WeaponProfileAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
