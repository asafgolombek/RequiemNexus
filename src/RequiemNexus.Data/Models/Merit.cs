using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

public class Merit
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // To represent if this merit scales. Examples: "1,2,3,4,5", or "3", or "1,3"
    [MaxLength(50)]
    public string ValidRatings { get; set; } = string.Empty;

    // Indicates if the player needs to type a specification, e.g. Language or Area of Expertise
    public bool RequiresSpecification { get; set; }

    // Indicates if a character can purchase this merit multiple times for different things
    public bool CanBePurchasedMultipleTimes { get; set; }
}
