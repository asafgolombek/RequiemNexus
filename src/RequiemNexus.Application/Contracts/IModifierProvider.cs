using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Supplies one slice of passive modifiers for a character. Composed by <see cref="IModifierService"/>.
/// </summary>
public interface IModifierProvider
{
    /// <summary>Stable sort key — lower values run first.</summary>
    int Order { get; }

    /// <summary>Primary source kind for this provider (diagnostics / registration).</summary>
    ModifierSourceType SourceType { get; }

    /// <summary>Loads modifiers contributed by this provider for the character.</summary>
    /// <param name="characterId">Character primary key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<PassiveModifier>> GetModifiersAsync(int characterId, CancellationToken cancellationToken = default);
}
