using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="ChronicleNpc"/>.
/// </summary>
public sealed class ChronicleNpcConfiguration : IEntityTypeConfiguration<ChronicleNpc>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChronicleNpc> builder)
    {
        builder
            .HasOne(n => n.Campaign)
            .WithMany(c => c.Npcs)
            .HasForeignKey(n => n.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(n => n.PrimaryFaction)
            .WithMany(f => f.Members)
            .HasForeignKey(n => n.PrimaryFactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.CampaignId);
        builder.HasIndex(n => n.PrimaryFactionId);
    }
}
