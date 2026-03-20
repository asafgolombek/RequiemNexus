using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="XpLedgerEntry"/>.
/// </summary>
public sealed class XpLedgerEntryConfiguration : IEntityTypeConfiguration<XpLedgerEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<XpLedgerEntry> builder)
    {
        builder
            .HasOne(x => x.Character)
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.CharacterId);
        builder.HasIndex(x => x.OccurredAt);
    }
}
