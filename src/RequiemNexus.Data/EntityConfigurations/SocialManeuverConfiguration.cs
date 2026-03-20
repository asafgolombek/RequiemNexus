using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="SocialManeuver"/>.
/// </summary>
public sealed class SocialManeuverConfiguration : IEntityTypeConfiguration<SocialManeuver>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SocialManeuver> builder)
    {
        builder
            .HasOne(m => m.Campaign)
            .WithMany(c => c.SocialManeuvers)
            .HasForeignKey(m => m.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(m => m.InitiatorCharacter)
            .WithMany(c => c.InitiatedSocialManeuvers)
            .HasForeignKey(m => m.InitiatorCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(m => m.TargetNpc)
            .WithMany(n => n.SocialManeuversTargeted)
            .HasForeignKey(m => m.TargetChronicleNpcId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.CampaignId);
        builder.HasIndex(m => m.InitiatorCharacterId);
        builder.HasIndex(m => m.TargetChronicleNpcId);
        builder.HasIndex(m => new { m.CampaignId, m.Status });
    }
}
