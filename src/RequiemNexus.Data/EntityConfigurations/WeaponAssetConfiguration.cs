using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// TPT table for <see cref="WeaponAsset"/>.
/// </summary>
public sealed class WeaponAssetConfiguration : IEntityTypeConfiguration<WeaponAsset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WeaponAsset> builder)
    {
        builder.ToTable("WeaponAssets");
    }
}
