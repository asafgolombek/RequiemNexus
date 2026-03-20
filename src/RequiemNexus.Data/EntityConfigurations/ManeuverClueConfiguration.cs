using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="ManeuverClue"/>.
/// </summary>
public sealed class ManeuverClueConfiguration : IEntityTypeConfiguration<ManeuverClue>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ManeuverClue> builder)
    {
        builder
            .HasOne(c => c.SocialManeuver)
            .WithMany(m => m.Clues)
            .HasForeignKey(c => c.SocialManeuverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.SocialManeuverId);
    }
}
