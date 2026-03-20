using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterDevotion"/>.
/// </summary>
public sealed class CharacterDevotionConfiguration : IEntityTypeConfiguration<CharacterDevotion>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterDevotion> builder)
    {
        builder
            .HasOne(cd => cd.Character)
            .WithMany(c => c.Devotions)
            .HasForeignKey(cd => cd.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cd => cd.DevotionDefinition)
            .WithMany()
            .HasForeignKey(cd => cd.DevotionDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cd => cd.CharacterId);
        builder.HasIndex(cd => cd.DevotionDefinitionId);
    }
}
