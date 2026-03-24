using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain;

/// <summary>
/// Maps combat <see cref="DamageSource"/> values to health-track severities and wire symbols.
/// </summary>
public static class HealthDamageKindExtensions
{
    /// <summary>
    /// Returns the health-track kind used when applying damage from the given source.
    /// </summary>
    /// <param name="source">Combat or environmental damage tag.</param>
    public static HealthDamageKind ToHealthDamageKind(this DamageSource source) =>
        source switch
        {
            DamageSource.Bashing => HealthDamageKind.Bashing,
            DamageSource.Lethal => HealthDamageKind.Lethal,
            DamageSource.Weapon => HealthDamageKind.Lethal,
            DamageSource.Aggravated => HealthDamageKind.Aggravated,
            DamageSource.Fire => HealthDamageKind.Aggravated,
            DamageSource.Sunlight => HealthDamageKind.Aggravated,
            _ => HealthDamageKind.Lethal,
        };

    /// <summary>
    /// Single-character encoding used by the Blazor vitals UI and APIs.
    /// </summary>
    /// <param name="kind">Severity to encode.</param>
    public static char ToTrackSymbol(this HealthDamageKind kind) =>
        kind switch
        {
            HealthDamageKind.Bashing => '/',
            HealthDamageKind.Lethal => 'X',
            HealthDamageKind.Aggravated => '*',
            _ => ' ',
        };
}
