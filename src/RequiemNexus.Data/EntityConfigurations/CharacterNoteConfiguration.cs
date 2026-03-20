using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterNote"/>.
/// </summary>
public sealed class CharacterNoteConfiguration : IEntityTypeConfiguration<CharacterNote>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterNote> builder)
    {
        builder
            .HasOne(n => n.Character)
            .WithMany()
            .HasForeignKey(n => n.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.CharacterId);
        builder.HasIndex(n => n.CampaignId);
    }
}
