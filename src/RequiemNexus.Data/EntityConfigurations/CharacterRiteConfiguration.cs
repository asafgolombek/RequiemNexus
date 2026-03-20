using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterRite"/>.
/// </summary>
public sealed class CharacterRiteConfiguration : IEntityTypeConfiguration<CharacterRite>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterRite> builder)
    {
        builder
            .HasOne(cr => cr.Character)
            .WithMany(c => c.Rites)
            .HasForeignKey(cr => cr.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cr => cr.SorceryRiteDefinition)
            .WithMany()
            .HasForeignKey(cr => cr.SorceryRiteDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cr => cr.CharacterId);
        builder.HasIndex(cr => cr.SorceryRiteDefinitionId);
    }
}
