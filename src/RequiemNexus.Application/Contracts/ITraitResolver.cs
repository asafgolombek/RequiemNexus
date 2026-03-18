using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Resolves a <see cref="PoolDefinition"/> to a dice pool integer by hydrating each trait from a character.
/// Phase 8: additive pools only.
/// </summary>
public interface ITraitResolver
{
    /// <summary>
    /// Resolves the pool definition to a single integer (sum of all trait ratings).
    /// The character must have Attributes, Skills, and Disciplines loaded.
    /// </summary>
    /// <param name="character">The character whose trait ratings to use.</param>
    /// <param name="pool">The pool definition (e.g., Strength + Brawl + Vigor).</param>
    /// <returns>The resolved dice pool (sum of trait ratings).</returns>
    int ResolvePool(RequiemNexus.Data.Models.Character character, PoolDefinition pool);
}
