using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="FactionRelationship"/>.
/// </summary>
public sealed class FactionRelationshipConfiguration : IEntityTypeConfiguration<FactionRelationship>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FactionRelationship> builder)
    {
        builder
            .HasOne(r => r.Campaign)
            .WithMany(c => c.FactionRelationships)
            .HasForeignKey(r => r.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(r => r.FactionA)
            .WithMany()
            .HasForeignKey(r => r.FactionAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(r => r.FactionB)
            .WithMany()
            .HasForeignKey(r => r.FactionBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.CampaignId);
    }
}
