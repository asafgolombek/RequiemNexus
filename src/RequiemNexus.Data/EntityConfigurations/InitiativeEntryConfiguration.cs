using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="InitiativeEntry"/>.
/// </summary>
public sealed class InitiativeEntryConfiguration : IEntityTypeConfiguration<InitiativeEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InitiativeEntry> builder)
    {
        builder
            .HasOne(i => i.Encounter)
            .WithMany(e => e.InitiativeEntries)
            .HasForeignKey(i => i.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.EncounterId);

        builder
            .HasOne(i => i.Character)
            .WithMany()
            .HasForeignKey(i => i.CharacterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(i => i.ChronicleNpc)
            .WithMany()
            .HasForeignKey(i => i.ChronicleNpcId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.CharacterId);

        builder
            .HasIndex(i => new { i.EncounterId, i.ChronicleNpcId })
            .IsUnique()
            .HasFilter("\"ChronicleNpcId\" IS NOT NULL");
    }
}
