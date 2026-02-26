using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public int? ClanId { get; set; }

    [ForeignKey(nameof(ClanId))]
    public virtual Clan? Clan { get; set; }

    public int? CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    // Core specific stats for the Neonate Phase
    public int Humanity { get; set; } = 7;
    public int Size { get; set; } = 5;
    public int ExperiencePoints { get; set; } = 0;
    public int Beats { get; set; } = 0;

    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }

    public int MaxWillpower { get; set; }
    public int CurrentWillpower { get; set; }

    public int BloodPotency { get; set; } = 1;

    public int MaxVitae { get; set; }
    public int CurrentVitae { get; set; }

    // --- Attributes ---
    // Mental
    public int Intelligence { get; set; } = 1;
    public int Wits { get; set; } = 1;
    public int Resolve { get; set; } = 1;

    // Physical
    public int Strength { get; set; } = 1;
    public int Dexterity { get; set; } = 1;
    public int Stamina { get; set; } = 1;

    // Social
    public int Presence { get; set; } = 1;
    public int Manipulation { get; set; } = 1;
    public int Composure { get; set; } = 1;

    // --- Skills ---
    // Mental Skills
    public int Academics { get; set; }
    public int Computer { get; set; }
    public int Crafts { get; set; }
    public int Investigation { get; set; }
    public int Medicine { get; set; }
    public int Occult { get; set; }
    public int Politics { get; set; }
    public int Science { get; set; }

    // Physical Skills
    public int Athletics { get; set; }
    public int Brawl { get; set; }
    public int Drive { get; set; }
    public int Firearms { get; set; }
    public int Larceny { get; set; }
    public int Stealth { get; set; }
    public int Survival { get; set; }
    public int Weaponry { get; set; }

    // Social Skills
    public int AnimalKen { get; set; }
    public int Empathy { get; set; }
    public int Expression { get; set; }
    public int Intimidation { get; set; }
    public int Persuasion { get; set; }
    public int Socialize { get; set; }
    public int Streetwise { get; set; }
    public int Subterfuge { get; set; }

    // --- Collections ---
    public virtual ICollection<CharacterMerit> Merits { get; set; } = new List<CharacterMerit>();
    public virtual ICollection<CharacterDiscipline> Disciplines { get; set; } = new List<CharacterDiscipline>();
}
