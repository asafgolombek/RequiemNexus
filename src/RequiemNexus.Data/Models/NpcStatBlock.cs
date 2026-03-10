namespace RequiemNexus.Data.Models;

/// <summary>
/// A reusable stat block for a chronicle NPC — either a pre-built canonical entry or a
/// campaign-specific custom block created by the Storyteller.
/// </summary>
public class NpcStatBlock
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the campaign this block belongs to.
    /// Null for pre-built (canonical) stat blocks visible to all campaigns.
    /// </summary>
    public int? CampaignId { get; set; }

    /// <summary>Gets or sets the display name of this stat block (e.g. "Mortal Thug").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-line concept description (e.g. "Street enforcer").</summary>
    public string Concept { get; set; } = string.Empty;

    /// <summary>Gets or sets the size rating (typically 5 for humans).</summary>
    public int Size { get; set; } = 5;

    /// <summary>Gets or sets the maximum Health boxes.</summary>
    public int Health { get; set; }

    /// <summary>Gets or sets the maximum Willpower dots.</summary>
    public int Willpower { get; set; }

    /// <summary>Gets or sets the general armor value against Bludgeoning damage.</summary>
    public int BludgeoningArmor { get; set; }

    /// <summary>Gets or sets the general armor value against Lethal damage.</summary>
    public int LethalArmor { get; set; }

    /// <summary>
    /// Gets or sets a JSON blob of attribute name/value pairs
    /// (e.g. {"Strength":2,"Dexterity":2,"Stamina":2,"Intelligence":2,"Wits":2,"Resolve":2,"Presence":2,"Manipulation":2,"Composure":2}).
    /// </summary>
    public string AttributesJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets a JSON blob of skill name/value pairs
    /// (e.g. {"Brawl":2,"Firearms":1}).
    /// </summary>
    public string SkillsJson { get; set; } = "{}";

    /// <summary>
    /// Gets or sets a JSON blob of discipline name/dot pairs
    /// (e.g. {"Dominate":2}).  Empty for mortals.
    /// </summary>
    public string DisciplinesJson { get; set; } = "{}";

    /// <summary>Gets or sets free-form Storyteller notes about tactics, appearance, etc.</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this stat block is a pre-built canonical entry.
    /// Pre-built blocks are read-only and cannot be deleted by campaign STs.
    /// </summary>
    public bool IsPrebuilt { get; set; }

    /// <summary>Gets or sets the optional navigation property to the owning campaign.</summary>
    public virtual Campaign? Campaign { get; set; }
}
