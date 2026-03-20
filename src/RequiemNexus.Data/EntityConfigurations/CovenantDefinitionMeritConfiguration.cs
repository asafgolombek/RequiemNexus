using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CovenantDefinitionMerit"/>.
/// </summary>
public sealed class CovenantDefinitionMeritConfiguration : IEntityTypeConfiguration<CovenantDefinitionMerit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CovenantDefinitionMerit> builder)
    {
        builder
            .HasOne(cdm => cdm.CovenantDefinition)
            .WithMany(c => c.CovenantSpecificMerits)
            .HasForeignKey(cdm => cdm.CovenantDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cdm => cdm.Merit)
            .WithMany()
            .HasForeignKey(cdm => cdm.MeritId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cdm => cdm.CovenantDefinitionId);
        builder.HasIndex(cdm => cdm.MeritId);
    }
}
