using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="EncounterTemplateNpc"/>.
/// </summary>
public sealed class EncounterTemplateNpcConfiguration : IEntityTypeConfiguration<EncounterTemplateNpc>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EncounterTemplateNpc> builder)
    {
        builder
            .HasOne(n => n.Template)
            .WithMany(t => t.Npcs)
            .HasForeignKey(n => n.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => n.TemplateId);
    }
}
