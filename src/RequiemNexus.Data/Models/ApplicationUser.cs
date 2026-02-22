using Microsoft.AspNetCore.Identity;

namespace RequiemNexus.Data.Models;

public class ApplicationUser : IdentityUser
{
    // A player or storyteller can have multiple characters
    public virtual ICollection<Character> Characters { get; set; } = new List<Character>();

    public DateOnly? Birthday { get; set; }
}
