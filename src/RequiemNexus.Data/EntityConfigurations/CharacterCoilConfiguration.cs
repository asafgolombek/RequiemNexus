using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterCoil"/>.
/// </summary>
public sealed class CharacterCoilConfiguration : IEntityTypeConfiguration<CharacterCoil>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterCoil> builder)
    {
        builder
            .HasOne(cc => cc.Character)
            .WithMany(c => c.Coils)
            .HasForeignKey(cc => cc.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cc => cc.CoilDefinition)
            .WithMany()
            .HasForeignKey(cc => cc.CoilDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cc => cc.CharacterId);
        builder.HasIndex(cc => cc.CoilDefinitionId);
    }
}
