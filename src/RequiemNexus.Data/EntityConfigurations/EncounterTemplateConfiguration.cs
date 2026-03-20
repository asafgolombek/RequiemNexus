using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="EncounterTemplate"/>.
/// </summary>
public sealed class EncounterTemplateConfiguration : IEntityTypeConfiguration<EncounterTemplate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EncounterTemplate> builder)
    {
        builder
            .HasOne(t => t.Campaign)
            .WithMany()
            .HasForeignKey(t => t.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.CampaignId);
    }
}
