using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Resolves a <see cref="PoolDefinition"/> to a dice pool integer by hydrating each trait from a character.
/// Phase 9: supports additive pools, penalty dice, lower-of, and modifier injection.
/// Untrained skills (0 dots) in the pool each apply −1 die before modifiers in <see cref="ResolvePool"/> (and thus in <see cref="ResolvePoolAsync"/>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Trait resolution inventory (O-11, 2026-04-05):</b> Base pool dice come only from <see cref="RequiemNexus.Domain.Enums.TraitType"/>
/// <c>Attribute</c>, <c>Skill</c>, and <c>Discipline</c> via synchronous reads on <see cref="RequiemNexus.Data.Models.Character"/>
/// (see <see cref="RequiemNexus.Application.Services.TraitResolver"/>). Conditional dice use <see cref="IModifierService"/> in
/// <see cref="ResolvePoolAsync"/>; no pool trait today requires a separate campaign- or session-scoped resolver. If a future trait needs
/// external context beyond modifiers, introduce an explicit contract (for example <c>IContextualTraitResolver</c>) rather than overloading this interface.
/// </para>
/// </remarks>
public interface ITraitResolver
{
    /// <summary>
    /// Resolves the pool definition to a single integer (sum of all trait ratings).
    /// Does not apply passive modifiers. Use <see cref="ResolvePoolAsync"/> for modifier-aware resolution.
    /// </summary>
    /// <param name="character">The character whose trait ratings to use.</param>
    /// <param name="pool">The pool definition (e.g., Strength + Brawl + Vigor).</param>
    /// <returns>The resolved dice pool.</returns>
    int ResolvePool(RequiemNexus.Data.Models.Character character, PoolDefinition pool);

    /// <summary>
    /// Resolves the pool definition with passive modifiers applied (e.g., +1 to Brawl).
    /// </summary>
    /// <param name="character">The character whose trait ratings to use.</param>
    /// <param name="pool">The pool definition.</param>
    /// <returns>The resolved dice pool including modifier deltas.</returns>
    Task<int> ResolvePoolAsync(RequiemNexus.Data.Models.Character character, PoolDefinition pool);
}
