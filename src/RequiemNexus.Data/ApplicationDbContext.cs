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
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CharacterMerit> CharacterMerits { get; set; }
    public DbSet<CharacterDiscipline> CharacterDisciplines { get; set; }

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
            .WithMany()
            .HasForeignKey(c => c.ClanId)
            .OnDelete(DeleteBehavior.SetNull);

        // Campaign >- StoryTeller configuration
        builder.Entity<Campaign>()
            .HasOne(c => c.StoryTeller)
            .WithMany()
            .HasForeignKey(c => c.StoryTellerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Character >- Campaign configuration
        builder.Entity<Character>()
            .HasOne(c => c.Campaign)
            .WithMany(c => c.Characters)
            .HasForeignKey(c => c.CampaignId)
            .OnDelete(DeleteBehavior.SetNull);

        // Character >- CharacterMerit configuration
        builder.Entity<CharacterMerit>()
            .HasOne(m => m.Character)
            .WithMany(c => c.Merits)
            .HasForeignKey(m => m.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Character >- CharacterDiscipline configuration
        builder.Entity<CharacterDiscipline>()
            .HasOne(d => d.Character)
            .WithMany(c => c.Disciplines)
            .HasForeignKey(d => d.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
