using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

public class Equipment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // e.g., Weapon, Armor, General

    [MaxLength(500)]
    public string? Description { get; set; }

    public float Weight { get; set; } // e.g., in lbs or kg

    // Cost in dots (• to •••••) or actual money value depending on house rules, sticking to typical CofD dots
    public int Cost { get; set; } 

    // Optional weapon or armor specific stats
    public int Damage { get; set; }
    public int ArmorRating { get; set; }
    public int Penalty { get; set; }
}
