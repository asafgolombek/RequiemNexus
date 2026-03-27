using System.ComponentModel;

namespace RequiemNexus.Domain.Enums;

public enum AttributeId
{
    [Description("Intelligence")]
    Intelligence,
    [Description("Wits")]
    Wits,
    [Description("Resolve")]
    Resolve,
    [Description("Strength")]
    Strength,
    [Description("Dexterity")]
    Dexterity,
    [Description("Stamina")]
    Stamina,
    [Description("Presence")]
    Presence,
    [Description("Manipulation")]
    Manipulation,
    [Description("Composure")]
    Composure,
}
