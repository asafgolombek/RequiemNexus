namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Damage severity on the character health track (bashing / lethal / aggravated).
/// </summary>
public enum HealthDamageKind
{
    /// <summary>Bashing damage, stored as '/' in <c>Character.HealthDamage</c>.</summary>
    Bashing,

    /// <summary>Lethal damage, stored as 'X'.</summary>
    Lethal,

    /// <summary>Aggravated damage, stored as '*'.</summary>
    Aggravated,
}
