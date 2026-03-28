using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="Discipline"/> acquisition metadata (Phase 19).
/// </summary>
public sealed class DisciplineConfiguration : IEntityTypeConfiguration<Discipline>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Discipline> builder)
    {
        builder.HasOne(d => d.Covenant)
            .WithMany()
            .HasForeignKey(d => d.CovenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Bloodline)
            .WithMany()
            .HasForeignKey(d => d.BloodlineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.CovenantId);
        builder.HasIndex(d => d.BloodlineId);
    }
}
