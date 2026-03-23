using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// TPT table for <see cref="ServiceAsset"/>.
/// </summary>
public sealed class ServiceAssetConfiguration : IEntityTypeConfiguration<ServiceAsset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ServiceAsset> builder)
    {
        builder.ToTable("ServiceAssets");
    }
}
