using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

public class Character
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(ApplicationUserId))]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Concept { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Mask { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Dirge { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Touchstone { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Height { get; set; } = string.Empty;

    [MaxLength(50)]
    public string EyeColor { get; set; } = string.Empty;

    [MaxLength(50)]
    public string HairColor { get; set; } = string.Empty;

    public string Backstory { get; set; } = string.Empty;

    public int? ClanId { get; set; }

    [ForeignKey(nameof(ClanId))]
    public virtual Clan? Clan { get; set; }

    /// <summary>Type of creature. Player-created characters default to Vampire. ST can set any type for NPCs.</summary>
    public Data.Models.Enums.CreatureType CreatureType { get; set; } = Data.Models.Enums.CreatureType.Vampire;

    /// <summary>Covenant membership. Null = Unaligned. Blocked for VII (antagonist covenant).</summary>
    public int? CovenantId { get; set; }

    [ForeignKey(nameof(CovenantId))]
    public virtual CovenantDefinition? Covenant { get; set; }

    /// <summary>When non-null, character applied to join; awaiting Storyteller approval. Null when Unaligned or already approved.</summary>
    public Data.Models.Enums.CovenantJoinStatus? CovenantJoinStatus { get; set; }

    /// <summary>When the covenant application was submitted. Null when not pending.</summary>
    public DateTime? CovenantAppliedAt { get; set; }

    /// <summary>When set, player has requested to leave; awaiting Storyteller approval. Cleared on approve or reject.</summary>
    public DateTime? CovenantLeaveRequestedAt { get; set; }

    /// <summary>Ordo Dracul: the approved chosen Mystery Scale. Null until approved by Storyteller.</summary>
    public int? ChosenMysteryScaleId { get; set; }

    [ForeignKey(nameof(ChosenMysteryScaleId))]
    public virtual ScaleDefinition? ChosenMysteryScale { get; set; }

    /// <summary>Ordo Dracul: pending chosen Mystery Scale awaiting Storyteller approval. Null when no pending request.</summary>
    public int? PendingChosenMysteryScaleId { get; set; }

    [ForeignKey(nameof(PendingChosenMysteryScaleId))]
    public virtual ScaleDefinition? PendingChosenMysteryScale { get; set; }

    /// <summary>Ordo Dracul: ST-approved Crucible Ritual access. Reduces Coil XP costs when true.</summary>
    public bool HasCrucibleRitualAccess { get; set; }

    public int? CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    /// <summary>Linked PC sire, when present.</summary>
    public int? SireCharacterId { get; set; }

    [ForeignKey(nameof(SireCharacterId))]
    public virtual Character? SireCharacter { get; set; }

    /// <summary>Linked NPC sire, when present.</summary>
    public int? SireNpcId { get; set; }

    [ForeignKey(nameof(SireNpcId))]
    public virtual ChronicleNpc? SireNpc { get; set; }

    /// <summary>Free-text sire when no PC/NPC link exists.</summary>
    [MaxLength(150)]
    public string? SireDisplayName { get; set; }

    // Core specific stats for the Neonate Phase
    public int Humanity { get; set; } = 7;

    /// <summary>Accumulated Humanity stains (Phase 9.5). Degeneration rolls remain at the Storyteller's discretion.</summary>
    public int HumanityStains { get; set; }

    public int Size { get; set; } = 5;

    public int ExperiencePoints { get; set; }

    public int TotalExperiencePoints { get; set; }

    public int Beats { get; set; }

    public int MaxHealth { get; set; }

    public int CurrentHealth { get; set; }

    [MaxLength(50)]
    public string HealthDamage { get; set; } = string.Empty;

    public int MaxWillpower { get; set; }

    public int CurrentWillpower { get; set; }

    public int BloodPotency { get; set; } = 1;

    public int MaxVitae { get; set; }

    public int CurrentVitae { get; set; }

    /// <summary>UTC when the character entered torpor; null when not in torpor.</summary>
    public DateTime? TorporSince { get; set; }

    /// <summary>UTC of the last starvation milestone notification while in torpor.</summary>
    public DateTime? LastStarvationNotifiedAt { get; set; }

    /// <summary>True while the character is in torpor.</summary>
    [NotMapped]
    public bool IsInTorpor => TorporSince.HasValue;

    // --- Derived Stats ---
    [NotMapped]
    public int CalculatedMaxHealth => Size + GetAttributeRating(AttributeId.Stamina);

    [NotMapped]
    public int CalculatedMaxWillpower => GetAttributeRating(AttributeId.Resolve) + GetAttributeRating(AttributeId.Composure);

    [NotMapped]
    public int Speed => GetAttributeRating(AttributeId.Strength) + GetAttributeRating(AttributeId.Dexterity) + Size
                        + CharacterAssets.Where(CharacterAssetActiveHelper.IsEquippedAndActive).Sum(ca => (ca.Asset as ArmorAsset)?.ArmorSpeedModifier ?? 0);

    [NotMapped]
    public int Defense => Math.Min(GetAttributeRating(AttributeId.Wits), GetAttributeRating(AttributeId.Dexterity))
                          + GetSkillRating(SkillId.Athletics)
                          + CharacterAssets.Where(CharacterAssetActiveHelper.IsEquippedAndActive).Sum(ca => (ca.Asset as ArmorAsset)?.ArmorDefenseModifier ?? 0);

    /// <summary>General armor rating from equipped armor only (slash/impact).</summary>
    [NotMapped]
    public int Armor => CharacterAssets.Where(CharacterAssetActiveHelper.IsEquippedAndActive).Sum(ca => (ca.Asset as ArmorAsset)?.ArmorRating ?? 0);

    /// <summary>Ballistic armor from equipped armor only.</summary>
    [NotMapped]
    public int BallisticArmor => CharacterAssets.Where(CharacterAssetActiveHelper.IsEquippedAndActive).Sum(ca => (ca.Asset as ArmorAsset)?.ArmorBallisticRating ?? 0);

    // --- Collections ---
    public virtual ICollection<CharacterAttribute> Attributes { get; set; } = new List<CharacterAttribute>();

    public virtual ICollection<CharacterSkill> Skills { get; set; } = new List<CharacterSkill>();

    public virtual ICollection<CharacterAspiration> Aspirations { get; set; } = new List<CharacterAspiration>();

    public virtual ICollection<CharacterBane> Banes { get; set; } = new List<CharacterBane>();

    public virtual ICollection<CharacterMerit> Merits { get; set; } = new List<CharacterMerit>();

    public virtual ICollection<CharacterDiscipline> Disciplines { get; set; } = new List<CharacterDiscipline>();

    public virtual ICollection<CharacterBloodline> Bloodlines { get; set; } = new List<CharacterBloodline>();

    public virtual ICollection<CharacterDevotion> Devotions { get; set; } = new List<CharacterDevotion>();

    public virtual ICollection<CharacterRite> Rites { get; set; } = new List<CharacterRite>();

    public virtual ICollection<CharacterCoil> Coils { get; set; } = new List<CharacterCoil>();

    public virtual ICollection<CharacterAsset> CharacterAssets { get; set; } = new List<CharacterAsset>();

    public virtual ICollection<SocialManeuver> InitiatedSocialManeuvers { get; set; } = new List<SocialManeuver>();

    public virtual ICollection<Character> Childer { get; set; } = new List<Character>();

    public virtual ICollection<BloodBond> BloodBondsAsThrall { get; set; } = new List<BloodBond>();

    public virtual ICollection<BloodBond> BloodBondsAsRegnant { get; set; } = new List<BloodBond>();

    public virtual ICollection<PredatoryAuraContest> PredatoryAuraContestsAsAttacker { get; set; } =
        new List<PredatoryAuraContest>();

    public virtual ICollection<PredatoryAuraContest> PredatoryAuraContestsAsDefender { get; set; } =
        new List<PredatoryAuraContest>();

    public virtual ICollection<PredatoryAuraContest> PredatoryAuraContestsAsWinner { get; set; } =
        new List<PredatoryAuraContest>();

    public virtual ICollection<Ghoul> GhoulsAsRegnant { get; set; } = new List<Ghoul>();

    /// <summary>Gets or sets a value indicating whether the character is retired from campaign play.
    /// Retired characters stay on the campaign roster for historical reference.</summary>
    public bool IsRetired { get; set; }

    /// <summary>Gets or sets the UTC date-time when the character was retired, or null if active.</summary>
    public DateTime? RetiredAt { get; set; }

    /// <summary>Gets or sets a value indicating whether the character has been globally archived by the owner.
    /// Archived characters are hidden from the active character list.</summary>
    public bool IsArchived { get; set; }

    /// <summary>Gets or sets the UTC date-time when the character was archived, or null if not archived.</summary>
    public DateTime? ArchivedAt { get; set; }

    // --- Helpers for accessing traits by name ---
    public int GetAttributeRating(AttributeId id) => GetAttributeRating(id.ToString());

    public int GetAttributeRating(string name)
    {
        return Attributes.FirstOrDefault(a => a.Name == name)?.Rating ?? 1;
    }

    public int GetSkillRating(SkillId id) => GetSkillRating(id.ToString());

    public int GetSkillRating(string name)
    {
        return Skills.FirstOrDefault(s => s.Name == name)?.Rating ?? 0;
    }

    /// <summary>Gets the rating for a Discipline by its ID. Returns 0 if the character does not have the discipline.</summary>
    public int GetDisciplineRating(int disciplineId)
    {
        return Disciplines.FirstOrDefault(d => d.DisciplineId == disciplineId)?.Rating ?? 0;
    }

    /// <summary>Gets the rating for a Discipline by name. Returns 0 if the character does not have the discipline.</summary>
    public int GetDisciplineRating(string name)
    {
        return Disciplines.FirstOrDefault(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase))?.Rating ?? 0;
    }

    /// <summary>
    /// Checks if a discipline is in-clan for this character.
    /// In-clan disciplines include the three standard clan disciplines plus the fourth discipline from an active bloodline.
    /// </summary>
    /// <param name="disciplineId">The ID of the discipline to check.</param>
    /// <returns>True if the discipline is in-clan; otherwise, false.</returns>
    public bool IsDisciplineInClan(int disciplineId)
    {
        // 1. Check parent clan disciplines
        if (Clan?.ClanDisciplines.Any(cd => cd.DisciplineId == disciplineId) == true)
        {
            return true;
        }

        // 2. Check active bloodline fourth discipline
        var activeBloodline = Bloodlines.FirstOrDefault(b => b.Status == Enums.BloodlineStatus.Active);
        if (activeBloodline?.BloodlineDefinition?.FourthDisciplineId == disciplineId)
        {
            return true;
        }

        return false;
    }
}
