using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Character> Characters { get; set; }
    public DbSet<Clan> Clans { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Character >- ApplicationUser configuration
        builder.Entity<Character>()
            .HasOne(c => c.User)
            .WithMany(u => u.Characters)
            .HasForeignKey(c => c.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Character >- Clan configuration
        builder.Entity<Character>()
            .HasOne(c => c.Clan)
            .WithMany(cl => cl.Characters)
            .HasForeignKey(c => c.ClanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
