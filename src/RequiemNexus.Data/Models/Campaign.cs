using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class Campaign
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string StoryTellerId { get; set; } = string.Empty;

    [ForeignKey(nameof(StoryTellerId))]
    public virtual ApplicationUser? StoryTeller { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Character> Characters { get; set; } = new List<Character>();
}
