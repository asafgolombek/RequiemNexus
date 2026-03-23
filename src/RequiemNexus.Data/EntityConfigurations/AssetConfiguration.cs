using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// TPT root for <see cref="Asset"/>.
/// </summary>
public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");
        builder.HasIndex(a => a.Slug);
    }
}
