using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterBloodline"/>.
/// </summary>
public sealed class CharacterBloodlineConfiguration : IEntityTypeConfiguration<CharacterBloodline>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterBloodline> builder)
    {
        builder
            .HasOne(cb => cb.Character)
            .WithMany(c => c.Bloodlines)
            .HasForeignKey(cb => cb.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cb => cb.BloodlineDefinition)
            .WithMany()
            .HasForeignKey(cb => cb.BloodlineDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cb => cb.CharacterId);
        builder.HasIndex(cb => cb.BloodlineDefinitionId);
    }
}
