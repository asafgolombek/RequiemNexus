using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="BeatLedgerEntry"/>.
/// </summary>
public sealed class BeatLedgerEntryConfiguration : IEntityTypeConfiguration<BeatLedgerEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BeatLedgerEntry> builder)
    {
        builder
            .HasOne(b => b.Character)
            .WithMany()
            .HasForeignKey(b => b.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.CharacterId);
        builder.HasIndex(b => b.OccurredAt);
    }
}
