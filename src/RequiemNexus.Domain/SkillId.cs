using System.ComponentModel;

namespace RequiemNexus.Domain;

public enum SkillId
{
    [Description("Academics")]
    Academics,
    [Description("Computer")]
    Computer,
    [Description("Crafts")]
    Crafts,
    [Description("Investigation")]
    Investigation,
    [Description("Medicine")]
    Medicine,
    [Description("Occult")]
    Occult,
    [Description("Politics")]
    Politics,
    [Description("Science")]
    Science,
    [Description("Athletics")]
    Athletics,
    [Description("Brawl")]
    Brawl,
    [Description("Drive")]
    Drive,
    [Description("Firearms")]
    Firearms,
    [Description("Larceny")]
    Larceny,
    [Description("Stealth")]
    Stealth,
    [Description("Survival")]
    Survival,
    [Description("Weaponry")]
    Weaponry,
    [Description("Animal Ken")]
    AnimalKen,
    [Description("Empathy")]
    Empathy,
    [Description("Expression")]
    Expression,
    [Description("Intimidation")]
    Intimidation,
    [Description("Persuasion")]
    Persuasion,
    [Description("Socialize")]
    Socialize,
    [Description("Streetwise")]
    Streetwise,
    [Description("Subterfuge")]
    Subterfuge,
}
