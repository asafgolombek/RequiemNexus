using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="ManeuverInterceptor"/>.
/// </summary>
public sealed class ManeuverInterceptorConfiguration : IEntityTypeConfiguration<ManeuverInterceptor>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ManeuverInterceptor> builder)
    {
        builder
            .HasOne(i => i.SocialManeuver)
            .WithMany(m => m.Interceptors)
            .HasForeignKey(i => i.SocialManeuverId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(i => i.InterceptorCharacter)
            .WithMany(c => c.ManeuverInterceptions)
            .HasForeignKey(i => i.InterceptorCharacterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.SocialManeuverId, i.InterceptorCharacterId }).IsUnique();
    }
}
