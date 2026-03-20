using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterDiscipline"/>.
/// </summary>
public sealed class CharacterDisciplineConfiguration : IEntityTypeConfiguration<CharacterDiscipline>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterDiscipline> builder)
    {
        builder
            .HasOne(d => d.Character)
            .WithMany(c => c.Disciplines)
            .HasForeignKey(d => d.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.CharacterId);
        builder.HasIndex(d => d.DisciplineId);
    }
}
