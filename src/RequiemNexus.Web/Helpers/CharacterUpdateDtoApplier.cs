using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Web.Helpers;

/// <summary>
/// Applies <see cref="CharacterUpdateDto"/> scalar fields onto an in-memory <see cref="Character"/> graph
/// (SignalR patches, embedded vitals, etc.).
/// </summary>
public static class CharacterUpdateDtoApplier
{
    /// <summary>
    /// Copies all present DTO fields onto <paramref name="character"/>.
    /// Does not mutate navigation collections (e.g. resolved Conditions).
    /// Ignores <see cref="CharacterUpdateDto.Armor"/> because <see cref="Character.Armor"/> is derived from equipment.
    /// </summary>
    public static void ApplyToCharacter(Character character, CharacterUpdateDto patch)
    {
        ArgumentNullException.ThrowIfNull(character);
        ArgumentNullException.ThrowIfNull(patch);

        if (patch.CurrentHealth.HasValue)
        {
            character.CurrentHealth = patch.CurrentHealth.Value;
        }

        if (patch.MaxHealth.HasValue)
        {
            character.MaxHealth = patch.MaxHealth.Value;
        }

        if (patch.HealthDamage != null)
        {
            character.HealthDamage = patch.HealthDamage;
        }

        if (patch.CurrentWillpower.HasValue)
        {
            character.CurrentWillpower = patch.CurrentWillpower.Value;
        }

        if (patch.MaxWillpower.HasValue)
        {
            character.MaxWillpower = patch.MaxWillpower.Value;
        }

        if (patch.CurrentVitae.HasValue)
        {
            character.CurrentVitae = patch.CurrentVitae.Value;
        }

        if (patch.MaxVitae.HasValue)
        {
            character.MaxVitae = patch.MaxVitae.Value;
        }

        if (patch.Humanity.HasValue)
        {
            character.Humanity = patch.Humanity.Value;
        }

        // Armor is computed from equipped assets on Character — not assignable.
        if (patch.Beats.HasValue)
        {
            character.Beats = patch.Beats.Value;
        }

        if (patch.ExperiencePoints.HasValue)
        {
            character.ExperiencePoints = patch.ExperiencePoints.Value;
        }

        if (patch.TotalExperiencePoints.HasValue)
        {
            character.TotalExperiencePoints = patch.TotalExperiencePoints.Value;
        }
    }
}
