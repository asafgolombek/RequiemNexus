using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// TPT table for <see cref="ArmorAsset"/>.
/// </summary>
public sealed class ArmorAssetConfiguration : IEntityTypeConfiguration<ArmorAsset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ArmorAsset> builder)
    {
        builder.ToTable("ArmorAssets");
    }
}
