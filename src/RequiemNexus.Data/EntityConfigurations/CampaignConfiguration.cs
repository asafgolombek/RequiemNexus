using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="Campaign"/>.
/// </summary>
public sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder
            .HasOne(c => c.StoryTeller)
            .WithMany()
            .HasForeignKey(c => c.StoryTellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.StoryTellerId);
    }
}
