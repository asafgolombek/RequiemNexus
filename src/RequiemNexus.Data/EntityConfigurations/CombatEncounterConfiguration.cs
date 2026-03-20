using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CombatEncounter"/>.
/// </summary>
public sealed class CombatEncounterConfiguration : IEntityTypeConfiguration<CombatEncounter>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CombatEncounter> builder)
    {
        builder
            .HasOne(e => e.Campaign)
            .WithMany()
            .HasForeignKey(e => e.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.CampaignId);
    }
}
