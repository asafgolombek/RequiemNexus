namespace RequiemNexus.Domain.Models;

/// <summary>
/// A declarative definition of a dice pool as a collection of trait references.
/// Phase 9: supports additive traits, penalty dice, lower-of, and contested rolls.
/// </summary>
/// <param name="Traits">The traits to sum for the pool (e.g., Strength + Brawl + Vigor).</param>
/// <param name="ContestedAgainst">Optional contested pool (e.g., vs Resolve + Composure). Used for display when target rolls manually.</param>
/// <param name="PenaltyTraits">Traits to subtract from the pool after summing (e.g., Pool - Stamina).</param>
/// <param name="LowerOf">Optional lower-of-two-traits contribution (e.g., min(Majesty, Dominate)).</param>
public record PoolDefinition(
    IReadOnlyList<TraitReference> Traits,
    PoolDefinition? ContestedAgainst = null,
    IReadOnlyList<TraitReference>? PenaltyTraits = null,
    LowerOfPair? LowerOf = null);
