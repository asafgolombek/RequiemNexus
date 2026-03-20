using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CampaignLore"/>.
/// </summary>
public sealed class CampaignLoreConfiguration : IEntityTypeConfiguration<CampaignLore>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CampaignLore> builder)
    {
        builder
            .HasOne(l => l.Campaign)
            .WithMany()
            .HasForeignKey(l => l.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => l.CampaignId);
    }
}
