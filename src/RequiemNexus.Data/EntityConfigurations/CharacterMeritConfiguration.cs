using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterMerit"/>.
/// </summary>
public sealed class CharacterMeritConfiguration : IEntityTypeConfiguration<CharacterMerit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterMerit> builder)
    {
        builder
            .HasOne(m => m.Character)
            .WithMany(c => c.Merits)
            .HasForeignKey(m => m.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.CharacterId);
    }
}
