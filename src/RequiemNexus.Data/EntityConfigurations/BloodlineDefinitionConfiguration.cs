using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="BloodlineDefinition"/>.
/// </summary>
public sealed class BloodlineDefinitionConfiguration : IEntityTypeConfiguration<BloodlineDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BloodlineDefinition> builder)
    {
        builder
            .HasOne(b => b.FourthDiscipline)
            .WithMany()
            .HasForeignKey(b => b.FourthDisciplineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.FourthDisciplineId);
    }
}
