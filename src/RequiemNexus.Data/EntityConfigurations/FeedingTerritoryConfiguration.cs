using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="FeedingTerritory"/>.
/// </summary>
public sealed class FeedingTerritoryConfiguration : IEntityTypeConfiguration<FeedingTerritory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FeedingTerritory> builder)
    {
        builder
            .HasOne(t => t.Campaign)
            .WithMany(c => c.Territories)
            .HasForeignKey(t => t.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(t => t.ControlledByFaction)
            .WithMany(f => f.ControlledTerritories)
            .HasForeignKey(t => t.ControlledByFactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.CampaignId);
        builder.HasIndex(t => t.ControlledByFactionId);
    }
}
