using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="HuntingRecord"/>.
/// </summary>
public sealed class HuntingRecordConfiguration : IEntityTypeConfiguration<HuntingRecord>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<HuntingRecord> builder)
    {
        builder
            .HasOne(r => r.Character)
            .WithMany()
            .HasForeignKey(r => r.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(r => r.Territory)
            .WithMany()
            .HasForeignKey(r => r.TerritoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.CharacterId);
        builder.HasIndex(r => r.HuntedAt);
    }
}
