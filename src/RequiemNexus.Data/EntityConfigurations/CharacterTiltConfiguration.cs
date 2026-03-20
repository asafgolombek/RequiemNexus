using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterTilt"/>.
/// </summary>
public sealed class CharacterTiltConfiguration : IEntityTypeConfiguration<CharacterTilt>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterTilt> builder)
    {
        builder
            .HasOne(t => t.Character)
            .WithMany()
            .HasForeignKey(t => t.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.CharacterId);
    }
}
