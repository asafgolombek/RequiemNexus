using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class ApplicationUser : IdentityUser
{
    // A player or storyteller can have multiple characters
    public virtual ICollection<Character> Characters { get; set; } = new List<Character>();

    // A storyteller can run multiple campaigns
    [InverseProperty("StoryTeller")]
    public virtual ICollection<Campaign> StoryToldCampaigns { get; set; } = new List<Campaign>();

    public DateOnly? Birthday { get; set; }
}
