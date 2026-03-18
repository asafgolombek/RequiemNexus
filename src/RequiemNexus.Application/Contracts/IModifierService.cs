using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Aggregates all active <see cref="PassiveModifier"/> records for a character at query time.
/// Modifiers are never applied permanently; derived values are computed on demand.
/// </summary>
public interface IModifierService
{
    /// <summary>
    /// Gets all active modifiers for a character (from Coils, Devotions, Covenant benefits, etc.).
    /// </summary>
    /// <param name="characterId">The character's ID.</param>
    /// <returns>All active modifiers, or empty if none apply.</returns>
    Task<IReadOnlyList<PassiveModifier>> GetModifiersForCharacterAsync(int characterId);
}
