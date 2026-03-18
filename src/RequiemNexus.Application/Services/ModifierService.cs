using RequiemNexus.Application.Contracts;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Aggregates passive modifiers from Coils, Devotions, Covenant benefits, etc.
/// Phase 9: Returns empty until Coils and Covenant benefits are implemented (Section 5).
/// </summary>
public class ModifierService : IModifierService
{
    /// <inheritdoc />
    public Task<IReadOnlyList<PassiveModifier>> GetModifiersForCharacterAsync(int characterId)
    {
        // Will aggregate from CharacterCoils, CharacterDevotions, Covenant when those exist.
        return Task.FromResult<IReadOnlyList<PassiveModifier>>([]);
    }
}
