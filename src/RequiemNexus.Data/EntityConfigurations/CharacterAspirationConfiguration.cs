using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterAspiration"/>.
/// </summary>
public sealed class CharacterAspirationConfiguration : IEntityTypeConfiguration<CharacterAspiration>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterAspiration> builder)
    {
        builder
            .HasOne(a => a.Character)
            .WithMany(c => c.Aspirations)
            .HasForeignKey(a => a.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.CharacterId);
    }
}
