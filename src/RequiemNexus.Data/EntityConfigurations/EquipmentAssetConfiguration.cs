using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// TPT table for <see cref="EquipmentAsset"/>.
/// </summary>
public sealed class EquipmentAssetConfiguration : IEntityTypeConfiguration<EquipmentAsset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EquipmentAsset> builder)
    {
        builder.ToTable("EquipmentAssets");
    }
}
