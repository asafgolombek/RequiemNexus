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

    public DbSet<CityFaction> CityFactions { get; set; } = default!;

    public DbSet<ChronicleNpc> ChronicleNpcs { get; set; } = default!;

    public DbSet<FeedingTerritory> FeedingTerritories { get; set; } = default!;

    public DbSet<FactionRelationship> FactionRelationships { get; set; } = default!;

    public DbSet<NpcStatBlock> NpcStatBlocks { get; set; } = default!;

    public DbSet<DiceMacro> DiceMacros { get; set; } = default!;

    public DbSet<CharacterNote> CharacterNotes { get; set; } = default!;

    public DbSet<CampaignLore> CampaignLore { get; set; } = default!;

    public DbSet<SessionPrepNote> SessionPrepNotes { get; set; } = default!;

    public DbSet<PublicRoll> PublicRolls { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Character >- ApplicationUser configuration
        builder.Entity<Character>()
            .HasOne(c => c.User)
            .WithMany(u => u.Characters)
            .HasForeignKey(c => c.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Character>()
            .HasIndex(c => c.ApplicationUserId);

        // Character >- Clan configuration
        builder.Entity<Character>()
            .HasOne(c => c.Clan)
            .WithMany()
            .HasForeignKey(c => c.ClanId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Character>()
            .HasIndex(c => c.ClanId);

        // Campaign >- StoryTeller configuration
        builder.Entity<Campaign>()
            .HasOne(c => c.StoryTeller)
            .WithMany()
            .HasForeignKey(c => c.StoryTellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Campaign>()
            .HasIndex(c => c.StoryTellerId);

        // Character >- Campaign configuration
        builder.Entity<Character>()
            .HasOne(c => c.Campaign)
            .WithMany(c => c.Characters)
            .HasForeignKey(c => c.CampaignId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Character>()
            .HasIndex(c => c.CampaignId);

        // Character >- CharacterMerit configuration
        builder.Entity<CharacterMerit>()
            .HasOne(m => m.Character)
            .WithMany(c => c.Merits)
            .HasForeignKey(m => m.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterMerit>()
            .HasIndex(m => m.CharacterId);

        // Character >- CharacterDiscipline configuration
        builder.Entity<CharacterDiscipline>()
            .HasOne(d => d.Character)
            .WithMany(c => c.Disciplines)
            .HasForeignKey(d => d.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterDiscipline>()
            .HasIndex(d => d.CharacterId);

        builder.Entity<CharacterDiscipline>()
            .HasIndex(d => d.DisciplineId);

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

        builder.Entity<ClanDiscipline>()
            .HasIndex(cd => cd.DisciplineId);

        // Character >- CharacterEquipment configuration
        builder.Entity<CharacterEquipment>()
            .HasOne(ce => ce.Character)
            .WithMany(c => c.CharacterEquipments)
            .HasForeignKey(ce => ce.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterEquipment>()
            .HasIndex(ce => ce.CharacterId);

        // Character >- CharacterAspiration
        builder.Entity<CharacterAspiration>()
            .HasOne(a => a.Character)
            .WithMany(c => c.Aspirations)
            .HasForeignKey(a => a.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterAspiration>()
            .HasIndex(a => a.CharacterId);

        // Character >- CharacterBane
        builder.Entity<CharacterBane>()
            .HasOne(b => b.Character)
            .WithMany(c => c.Banes)
            .HasForeignKey(b => b.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterBane>()
            .HasIndex(b => b.CharacterId);

        // UserSession >- ApplicationUser configuration
        builder.Entity<UserSession>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserSession>()
            .HasIndex(s => s.ApplicationUserId);

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

        builder.Entity<InitiativeEntry>()
            .HasIndex(i => i.CharacterId);

        // CityFaction >- Campaign
        builder.Entity<CityFaction>()
            .HasOne(f => f.Campaign)
            .WithMany(c => c.Factions)
            .HasForeignKey(f => f.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // CityFaction LeaderNpc self-reference (optional)
        builder.Entity<CityFaction>()
            .HasOne(f => f.LeaderNpc)
            .WithMany()
            .HasForeignKey(f => f.LeaderNpcId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<CityFaction>()
            .HasIndex(f => f.CampaignId);

        // ChronicleNpc >- Campaign
        builder.Entity<ChronicleNpc>()
            .HasOne(n => n.Campaign)
            .WithMany(c => c.Npcs)
            .HasForeignKey(n => n.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // ChronicleNpc >- PrimaryFaction (optional)
        builder.Entity<ChronicleNpc>()
            .HasOne(n => n.PrimaryFaction)
            .WithMany(f => f.Members)
            .HasForeignKey(n => n.PrimaryFactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<ChronicleNpc>()
            .HasIndex(n => n.CampaignId);

        builder.Entity<ChronicleNpc>()
            .HasIndex(n => n.PrimaryFactionId);

        // FeedingTerritory >- Campaign
        builder.Entity<FeedingTerritory>()
            .HasOne(t => t.Campaign)
            .WithMany(c => c.Territories)
            .HasForeignKey(t => t.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // FeedingTerritory >- ControlledByFaction (optional)
        builder.Entity<FeedingTerritory>()
            .HasOne(t => t.ControlledByFaction)
            .WithMany(f => f.ControlledTerritories)
            .HasForeignKey(t => t.ControlledByFactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<FeedingTerritory>()
            .HasIndex(t => t.CampaignId);

        builder.Entity<FeedingTerritory>()
            .HasIndex(t => t.ControlledByFactionId);

        // FactionRelationship >- Campaign
        builder.Entity<FactionRelationship>()
            .HasOne(r => r.Campaign)
            .WithMany(c => c.FactionRelationships)
            .HasForeignKey(r => r.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        // FactionRelationship >- FactionA / FactionB (no cascade to avoid cycles)
        builder.Entity<FactionRelationship>()
            .HasOne(r => r.FactionA)
            .WithMany()
            .HasForeignKey(r => r.FactionAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FactionRelationship>()
            .HasOne(r => r.FactionB)
            .WithMany()
            .HasForeignKey(r => r.FactionBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FactionRelationship>()
            .HasIndex(r => r.CampaignId);

        // NpcStatBlock >- Campaign (optional — null for prebuilt blocks)
        builder.Entity<NpcStatBlock>()
            .HasOne(s => s.Campaign)
            .WithMany()
            .HasForeignKey(s => s.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<NpcStatBlock>()
            .HasIndex(s => s.CampaignId);

        builder.Entity<NpcStatBlock>()
            .HasIndex(s => s.IsPrebuilt);

        // DiceMacro >- Character
        builder.Entity<DiceMacro>()
            .HasOne(m => m.Character)
            .WithMany()
            .HasForeignKey(m => m.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<DiceMacro>()
            .HasIndex(m => m.CharacterId);

        // CharacterNote >- Character
        builder.Entity<CharacterNote>()
            .HasOne(n => n.Character)
            .WithMany()
            .HasForeignKey(n => n.CharacterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CharacterNote>()
            .HasIndex(n => n.CharacterId);

        builder.Entity<CharacterNote>()
            .HasIndex(n => n.CampaignId);

        // CampaignLore >- Campaign
        builder.Entity<CampaignLore>()
            .HasOne(l => l.Campaign)
            .WithMany()
            .HasForeignKey(l => l.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CampaignLore>()
            .HasIndex(l => l.CampaignId);

        // SessionPrepNote >- Campaign
        builder.Entity<SessionPrepNote>()
            .HasOne(s => s.Campaign)
            .WithMany()
            .HasForeignKey(s => s.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SessionPrepNote>()
            .HasIndex(s => s.CampaignId);

        // PublicRoll configuration
        builder.Entity<PublicRoll>()
            .HasIndex(r => r.Slug)
            .IsUnique();

        builder.Entity<PublicRoll>()
            .HasOne(r => r.RolledByUser)
            .WithMany()
            .HasForeignKey(r => r.RolledByUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Merit configuration
        builder.Entity<Merit>()
            .HasIndex(m => m.Name)
            .IsUnique();
    }
}
