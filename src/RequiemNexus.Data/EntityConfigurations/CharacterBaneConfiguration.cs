using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterBane"/>.
/// </summary>
public sealed class CharacterBaneConfiguration : IEntityTypeConfiguration<CharacterBane>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterBane> builder)
    {
        builder
            .HasOne(b => b.Character)
            .WithMany(c => c.Banes)
            .HasForeignKey(b => b.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.CharacterId);
    }
}
