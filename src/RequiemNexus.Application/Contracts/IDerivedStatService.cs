using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Computes derived stats (Health, Speed, Defense) with passive modifiers applied.
/// Use instead of Character's computed properties when modifiers may apply.
/// </summary>
public interface IDerivedStatService
{
    /// <summary>Gets effective Defense (base + modifiers).</summary>
    Task<int> GetEffectiveDefenseAsync(Character character);

    /// <summary>Gets effective Speed (base + modifiers).</summary>
    Task<int> GetEffectiveSpeedAsync(Character character);

    /// <summary>Gets effective Max Health (base + modifiers).</summary>
    Task<int> GetEffectiveMaxHealthAsync(Character character);
}
