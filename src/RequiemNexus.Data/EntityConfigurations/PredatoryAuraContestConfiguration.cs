using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="PredatoryAuraContest"/>.
/// </summary>
public sealed class PredatoryAuraContestConfiguration : IEntityTypeConfiguration<PredatoryAuraContest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PredatoryAuraContest> builder)
    {
        builder
            .HasOne(p => p.Chronicle)
            .WithMany(c => c.PredatoryAuraContests)
            .HasForeignKey(p => p.ChronicleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(p => p.AttackerCharacter)
            .WithMany(c => c.PredatoryAuraContestsAsAttacker)
            .HasForeignKey(p => p.AttackerCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(p => p.DefenderCharacter)
            .WithMany(c => c.PredatoryAuraContestsAsDefender)
            .HasForeignKey(p => p.DefenderCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(p => p.WinnerCharacter)
            .WithMany(c => c.PredatoryAuraContestsAsWinner)
            .HasForeignKey(p => p.WinnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => p.ChronicleId);
        builder.HasIndex(p => p.AttackerCharacterId);
        builder.HasIndex(p => p.DefenderCharacterId);
    }
}
