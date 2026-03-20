using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterEquipment"/>.
/// </summary>
public sealed class CharacterEquipmentConfiguration : IEntityTypeConfiguration<CharacterEquipment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterEquipment> builder)
    {
        builder
            .HasOne(ce => ce.Character)
            .WithMany(c => c.CharacterEquipments)
            .HasForeignKey(ce => ce.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ce => ce.CharacterId);
    }
}
