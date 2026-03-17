namespace RequiemNexus.Domain.Models;

/// <summary>
/// A declarative definition of a dice pool as a collection of trait references.
/// Phase 8: additive pools only (simple sum). Contested and penalty dice deferred to Phase 9.
/// </summary>
/// <param name="Traits">The traits to sum for the pool (e.g., Strength + Brawl + Vigor).</param>
public record PoolDefinition(IReadOnlyList<TraitReference> Traits);
