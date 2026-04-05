using RequiemNexus.Application.Contracts;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Aggregates passive modifiers from registered <see cref="IModifierProvider"/> implementations.
/// When rules add DB-backed passive modifiers from Devotion, Covenant, Bloodline, or Merit state, add new providers (plan O-8) — do not extend this type with source-type conditionals.
/// </summary>
public sealed class ModifierService(IEnumerable<IModifierProvider> providers) : IModifierService
{
    private readonly IReadOnlyList<IModifierProvider> _ordered = providers.OrderBy(p => p.Order).ToList();

    /// <inheritdoc />
    public async Task<IReadOnlyList<PassiveModifier>> GetModifiersForCharacterAsync(int characterId)
    {
        var modifiers = new List<PassiveModifier>();
        foreach (IModifierProvider provider in _ordered)
        {
            modifiers.AddRange(await provider.GetModifiersAsync(characterId));
        }

        return modifiers.AsReadOnly();
    }
}
