using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Configuration for <see cref="AssetModifier"/>.
/// </summary>
public sealed class AssetModifierConfiguration : IEntityTypeConfiguration<AssetModifier>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AssetModifier> builder)
    {
        builder.ToTable("AssetModifiers");

        builder.HasIndex(a => a.Slug).IsUnique();
    }
}
