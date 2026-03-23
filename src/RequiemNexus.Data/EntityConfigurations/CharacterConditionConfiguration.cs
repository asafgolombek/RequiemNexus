using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="CharacterCondition"/>.
/// </summary>
public sealed class CharacterConditionConfiguration : IEntityTypeConfiguration<CharacterCondition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CharacterCondition> builder)
    {
        builder
            .HasOne(c => c.Character)
            .WithMany()
            .HasForeignKey(c => c.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.CharacterId, c.ConditionType, c.IsResolved, c.SourceTag });
    }
}
