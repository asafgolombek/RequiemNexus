using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.EntityConfigurations;

/// <summary>
/// Fluent configuration for <see cref="ConsentLog"/>.
/// </summary>
public sealed class ConsentLogConfiguration : IEntityTypeConfiguration<ConsentLog>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ConsentLog> builder)
    {
        builder
            .HasOne(c => c.User)
            .WithMany(u => u.ConsentLogs)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId);
    }
}
