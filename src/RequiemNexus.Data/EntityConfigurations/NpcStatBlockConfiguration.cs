using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="NpcStatBlock"/>.
/// </summary>
public sealed class NpcStatBlockConfiguration : IEntityTypeConfiguration<NpcStatBlock>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NpcStatBlock> builder)
    {
        builder
            .HasOne(s => s.Campaign)
            .WithMany()
            .HasForeignKey(s => s.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.CampaignId);
        builder.HasIndex(s => s.IsPrebuilt);
    }
}
