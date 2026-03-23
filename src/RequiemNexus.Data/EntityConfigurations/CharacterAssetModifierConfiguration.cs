using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Configuration for <see cref="CharacterAssetModifier"/>.
/// </summary>
public sealed class CharacterAssetModifierConfiguration : IEntityTypeConfiguration<CharacterAssetModifier>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterAssetModifier> builder)
    {
        builder.ToTable("CharacterAssetModifiers");

        builder.HasOne(m => m.CharacterAsset)
               .WithMany(a => a.Modifiers)
               .HasForeignKey(m => m.CharacterAssetId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.AssetModifier)
               .WithMany(am => am.AppliedTo)
               .HasForeignKey(m => m.AssetModifierId)
               .OnDelete(DeleteBehavior.Restrict);

        // Prevent the same upgrade from being applied twice to the same item.
        builder.HasIndex(m => new { m.CharacterAssetId, m.AssetModifierId }).IsUnique();
    }
}
