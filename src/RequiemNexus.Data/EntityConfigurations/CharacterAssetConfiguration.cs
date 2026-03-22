using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterAsset"/>.
/// </summary>
public sealed class CharacterAssetConfiguration : IEntityTypeConfiguration<CharacterAsset>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterAsset> builder)
    {
        builder
            .HasOne(ca => ca.Character)
            .WithMany(c => c.CharacterAssets)
            .HasForeignKey(ca => ca.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ca => ca.CharacterId);
        builder.HasIndex(ca => ca.AssetId);

        builder.Property(ca => ca.IsEquipped).HasDefaultValue(true);
    }
}
