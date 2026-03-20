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

    public DbSet<MeritPrerequisite> MeritPrerequisites { get; set; } = default!;

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

    public DbSet<BloodlineDefinition> BloodlineDefinitions { get; set; } = default!;

    public DbSet<BloodlineClan> BloodlineClans { get; set; } = default!;

    public DbSet<CharacterBloodline> CharacterBloodlines { get; set; } = default!;

    public DbSet<DevotionDefinition> DevotionDefinitions { get; set; } = default!;

    public DbSet<DevotionPrerequisite> DevotionPrerequisites { get; set; } = default!;

    public DbSet<CharacterDevotion> CharacterDevotions { get; set; } = default!;

    public DbSet<CovenantDefinition> CovenantDefinitions { get; set; } = default!;

    public DbSet<CovenantDefinitionMerit> CovenantDefinitionMerits { get; set; } = default!;

    public DbSet<SorceryRiteDefinition> SorceryRiteDefinitions { get; set; } = default!;

    public DbSet<CharacterRite> CharacterRites { get; set; } = default!;

    public DbSet<ScaleDefinition> ScaleDefinitions { get; set; } = default!;

    public DbSet<CoilDefinition> CoilDefinitions { get; set; } = default!;

    public DbSet<CharacterCoil> CharacterCoils { get; set; } = default!;

    public DbSet<SocialManeuver> SocialManeuvers { get; set; } = default!;

    public DbSet<ManeuverClue> ManeuverClues { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
