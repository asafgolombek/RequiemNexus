using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="UserSession"/>.
/// </summary>
public sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.ApplicationUserId);
    }
}
