namespace RequiemNexus.Domain.Models;

/// <summary>
/// Represents the "lower of two traits" mechanic for dice pools.
/// The contributed value is min(left, right).
/// </summary>
/// <param name="Left">The first trait.</param>
/// <param name="Right">The second trait.</param>
public record LowerOfPair(TraitReference Left, TraitReference Right);
