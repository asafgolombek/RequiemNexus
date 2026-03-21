using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="EncounterNpcTemplate"/>.
/// </summary>
public sealed class EncounterNpcTemplateConfiguration : IEntityTypeConfiguration<EncounterNpcTemplate>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EncounterNpcTemplate> builder)
    {
        builder
            .HasOne(t => t.Encounter)
            .WithMany(e => e.NpcTemplates)
            .HasForeignKey(t => t.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(t => t.ChronicleNpc)
            .WithMany()
            .HasForeignKey(t => t.ChronicleNpcId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.EncounterId);

        builder
            .HasIndex(t => new { t.EncounterId, t.ChronicleNpcId })
            .IsUnique()
            .HasFilter("\"ChronicleNpcId\" IS NOT NULL");
    }
}
