using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

public class Clan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;
}
