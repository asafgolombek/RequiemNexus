using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CityFaction"/>.
/// </summary>
public sealed class CityFactionConfiguration : IEntityTypeConfiguration<CityFaction>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CityFaction> builder)
    {
        builder
            .HasOne(f => f.Campaign)
            .WithMany(c => c.Factions)
            .HasForeignKey(f => f.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(f => f.LeaderNpc)
            .WithMany()
            .HasForeignKey(f => f.LeaderNpcId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(f => f.CampaignId);
    }
}
