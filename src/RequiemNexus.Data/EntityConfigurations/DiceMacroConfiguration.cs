using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="DiceMacro"/>.
/// </summary>
public sealed class DiceMacroConfiguration : IEntityTypeConfiguration<DiceMacro>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DiceMacro> builder)
    {
        builder
            .HasOne(m => m.Character)
            .WithMany()
            .HasForeignKey(m => m.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.CharacterId);
    }
}
