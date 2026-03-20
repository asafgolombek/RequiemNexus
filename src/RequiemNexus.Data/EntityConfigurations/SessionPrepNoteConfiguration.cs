using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="SessionPrepNote"/>.
/// </summary>
public sealed class SessionPrepNoteConfiguration : IEntityTypeConfiguration<SessionPrepNote>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SessionPrepNote> builder)
    {
        builder
            .HasOne(s => s.Campaign)
            .WithMany()
            .HasForeignKey(s => s.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.CampaignId);
    }
}
