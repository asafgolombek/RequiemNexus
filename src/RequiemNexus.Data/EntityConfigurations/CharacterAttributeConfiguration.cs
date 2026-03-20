using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterAttribute"/>.
/// </summary>
public sealed class CharacterAttributeConfiguration : IEntityTypeConfiguration<CharacterAttribute>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterAttribute> builder)
    {
        builder
            .HasOne(a => a.Character)
            .WithMany(c => c.Attributes)
            .HasForeignKey(a => a.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.CharacterId, a.Name }).IsUnique();
    }
}
