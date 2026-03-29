namespace RequiemNexus.Domain.Models;

/// <summary>
/// Typed representation of a Discipline power's activation cost.
/// Parsed from the seeded <c>Cost</c> string on a discipline power (e.g. "1 Vitae", "1 Willpower", "—").
/// </summary>
/// <param name="Type">Which resource is spent, if any.</param>
/// <param name="Amount">How many points or dots are spent when not <see cref="ActivationCostType.None"/>.</param>
public sealed record ActivationCost(ActivationCostType Type, int Amount)
{
    /// <summary>Zero-cost power ("—", empty, or null).</summary>
    public static readonly ActivationCost None = new(ActivationCostType.None, 0);

    /// <summary>Gets a value indicating whether this power is free to activate.</summary>
    public bool IsNone => Type == ActivationCostType.None;

    /// <summary>
    /// Parses a cost string such as "1 Vitae", "2 Vitae", "1 Willpower",
    /// "1 Vitae or 1 Willpower", "—", or empty/null.
    /// Returns <see cref="None"/> for unrecognised or empty strings.
    /// </summary>
    /// <param name="costString">The raw cost column from seed data.</param>
    /// <returns>A parsed cost, or <see cref="None"/> when not recognised.</returns>
    public static ActivationCost Parse(string? costString)
    {
        if (string.IsNullOrWhiteSpace(costString))
        {
            return None;
        }

        var trimmed = costString.Trim().TrimStart('—', '-', '–');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return None;
        }

        // Handle "N Vitae or N Willpower" — default to Vitae (see rules-interpretations.md)
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return None;
        }

        int amount = int.TryParse(parts[0], out var parsed) ? parsed : 1;
        return parts[1].ToLowerInvariant() switch
        {
            "vitae" => new ActivationCost(ActivationCostType.Vitae, amount),
            "willpower" => new ActivationCost(ActivationCostType.Willpower, amount),
            _ => None,
        };
    }
}
