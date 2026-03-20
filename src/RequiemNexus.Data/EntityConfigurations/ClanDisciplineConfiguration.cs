using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="ClanDiscipline"/>.
/// </summary>
public sealed class ClanDisciplineConfiguration : IEntityTypeConfiguration<ClanDiscipline>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ClanDiscipline> builder)
    {
        builder
            .HasOne(cd => cd.Clan)
            .WithMany(c => c.ClanDisciplines)
            .HasForeignKey(cd => cd.ClanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cd => cd.Discipline)
            .WithMany()
            .HasForeignKey(cd => cd.DisciplineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cd => cd.DisciplineId);
    }
}
