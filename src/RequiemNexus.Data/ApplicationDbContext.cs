using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Character> Characters { get; set; } = default!;

    public DbSet<Clan> Clans { get; set; } = default!;

    public DbSet<Campaign> Campaigns { get; set; } = default!;

    public DbSet<UserSession> UserSessions { get; set; } = default!;

    public DbSet<FidoStoredCredential> FidoStoredCredentials { get; set; } = null!;

    public DbSet<CharacterMerit> CharacterMerits { get; set; } = default!;

    public DbSet<CharacterDiscipline> CharacterDisciplines { get; set; } = default!;

    public DbSet<CharacterAttribute> CharacterAttributes { get; set; } = default!;

    public DbSet<CharacterSkill> CharacterSkills { get; set; } = default!;

    public DbSet<Discipline> Disciplines { get; set; } = default!;

    public DbSet<DisciplinePower> DisciplinePowers { get; set; } = default!;

    public DbSet<Merit> Merits { get; set; } = default!;

    public DbSet<ClanDiscipline> ClanDisciplines { get; set; } = default!;

    public DbSet<Equipment> Equipment { get; set; } = default!;

    public DbSet<CharacterEquipment> CharacterEquipments { get; set; } = default!;

    public DbSet<CharacterAspiration> CharacterAspirations { get; set; } = default!;

    public DbSet<CharacterBane> CharacterBanes { get; set; } = default!;

    public DbSet<AuditLog> AuditLogs { get; set; } = default!;

    public DbSet<ConsentLog> ConsentLogs { get; set; } = default!;

    public DbSet<BeatLedgerEntry> BeatLedger { get; set; } = default!;

    public DbSet<XpLedgerEntry> XpLedger { get; set; } = default!;

    public DbSet<CharacterCondition> CharacterConditions { get; set; } = default!;

    public DbSet<CharacterTilt> CharacterTilts { get; set; } = default!;

    public DbSet<CombatEncounter> CombatEncounters { get; set; } = default!;

    public DbSet<InitiativeEntry> InitiativeEntries { get; set; } = default!;

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

        // Clan >- ClanDiscipline configuration
        builder.Entity<ClanDiscipline>()
            .HasOne(cd => cd.Clan)
            .WithMany(c => c.ClanDisciplines)
            .HasForeignKey(cd => cd.ClanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClanDiscipline>()
            .HasOne(cd => cd.Discipline)
            .WithMany()
            .HasForeignKey(cd => cd.DisciplineId)
            .OnDelete(DeleteBehavior.Cascade);

        // Character >- CharacterEquipment configuration
        builder.Entity<CharacterEquipment>()
            .HasOne(ce => ce.Character)
            .WithMany(c => c.CharacterEquipments)
            .HasForeignKey(ce => ce.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Character >- CharacterAspiration
        builder.Entity<CharacterAspiration>()
            .HasOne(a => a.Character)
            .WithMany(c => c.Aspirations)
            .HasForeignKey(a => a.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        // Character >- CharacterBane
        builder.Entity<CharacterBane>()
            .HasOne(b => b.Character)
            .WithMany(c => c.Banes)
            .HasForeignKey(b => b.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserSession >- ApplicationUser configuration
        builder.Entity<UserSession>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // AuditLog >- ApplicationUser configuration
        builder.Entity<AuditLog>()
            .HasOne(l => l.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AuditLog>()
            .HasIndex(l => l.UserId);

        builder.Entity<AuditLog>()
            .HasIndex(l => l.OccurredAt);

        // ConsentLog >- ApplicationUser configuration
        builder.Entity<ConsentLog>()
            .HasOne(c => c.User)
            .WithMany(u => u.ConsentLogs)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ConsentLog>()
            .HasIndex(c => c.UserId);

        // Character >- CharacterAttribute configuration
        builder.Entity<CharacterAttribute>()
            .HasOne(a => a.Character)
            .WithMany(c => c.Attributes)
            .HasForeignKey(a => a.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterAttribute>()
            .HasIndex(a => new { a.CharacterId, a.Name })
            .IsUnique();

        // Character >- CharacterSkill configuration
        builder.Entity<CharacterSkill>()
            .HasOne(s => s.Character)
            .WithMany(c => c.Skills)
            .HasForeignKey(s => s.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterSkill>()
            .HasIndex(s => new { s.CharacterId, s.Name })
            .IsUnique();

        // BeatLedgerEntry >- Character
        builder.Entity<BeatLedgerEntry>()
            .HasOne(b => b.Character)
            .WithMany()
            .HasForeignKey(b => b.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BeatLedgerEntry>()
            .HasIndex(b => b.CharacterId);

        builder.Entity<BeatLedgerEntry>()
            .HasIndex(b => b.OccurredAt);

        // XpLedgerEntry >- Character
        builder.Entity<XpLedgerEntry>()
            .HasOne(x => x.Character)
            .WithMany()
            .HasForeignKey(x => x.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<XpLedgerEntry>()
            .HasIndex(x => x.CharacterId);

        builder.Entity<XpLedgerEntry>()
            .HasIndex(x => x.OccurredAt);

        // CharacterCondition >- Character
        builder.Entity<CharacterCondition>()
            .HasOne(c => c.Character)
            .WithMany()
            .HasForeignKey(c => c.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterCondition>()
            .HasIndex(c => c.CharacterId);

        // CharacterTilt >- Character
        builder.Entity<CharacterTilt>()
            .HasOne(t => t.Character)
            .WithMany()
            .HasForeignKey(t => t.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterTilt>()
            .HasIndex(t => t.CharacterId);

        // CombatEncounter >- Campaign
        builder.Entity<CombatEncounter>()
            .HasOne(e => e.Campaign)
            .WithMany()
            .HasForeignKey(e => e.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CombatEncounter>()
            .HasIndex(e => e.CampaignId);

        // InitiativeEntry >- CombatEncounter
        builder.Entity<InitiativeEntry>()
            .HasOne(i => i.Encounter)
            .WithMany(e => e.InitiativeEntries)
            .HasForeignKey(i => i.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InitiativeEntry>()
            .HasIndex(i => i.EncounterId);

        // InitiativeEntry >- Character (optional FK, null for NPCs)
        builder.Entity<InitiativeEntry>()
            .HasOne(i => i.Character)
            .WithMany()
            .HasForeignKey(i => i.CharacterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
