using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterSkill"/>.
/// </summary>
public sealed class CharacterSkillConfiguration : IEntityTypeConfiguration<CharacterSkill>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterSkill> builder)
    {
        builder
            .HasOne(s => s.Character)
            .WithMany(c => c.Skills)
            .HasForeignKey(s => s.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.CharacterId, s.Name }).IsUnique();
    }
}
