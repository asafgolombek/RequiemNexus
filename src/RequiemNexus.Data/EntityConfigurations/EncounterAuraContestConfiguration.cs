using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="EncounterAuraContest"/>.
/// </summary>
public sealed class EncounterAuraContestConfiguration : IEntityTypeConfiguration<EncounterAuraContest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EncounterAuraContest> builder)
    {
        builder
            .HasOne(e => e.Encounter)
            .WithMany(c => c.EncounterAuraContests)
            .HasForeignKey(e => e.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.VampireLower)
            .WithMany()
            .HasForeignKey(e => e.VampireLowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.VampireHigher)
            .WithMany()
            .HasForeignKey(e => e.VampireHigherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.EncounterId, e.VampireLowerId, e.VampireHigherId }).IsUnique();
    }
}
