using System.Text.RegularExpressions;

namespace RequiemNexus.Domain.Models;

/// <summary>
/// Typed representation of a Discipline power's activation cost.
/// Parsed from the seeded <c>Cost</c> string on a discipline power (e.g. "1 Vitae", "1 Willpower", "—").
/// </summary>
/// <param name="Type">Which resource is spent when not <see cref="ActivationCostType.None"/> and not a player choice.</param>
/// <param name="Amount">Vitae amount, single Willpower amount, or Vitae leg of an <c>or</c> cost.</param>
/// <param name="IsPlayerChoiceVitaeOrWillpower">True when the player must pick Vitae or Willpower in the UI.</param>
/// <param name="PlayerChoiceWillpowerAmount">Willpower dots for the Willpower leg when <paramref name="IsPlayerChoiceVitaeOrWillpower"/> is true.</param>
public sealed record ActivationCost(
    ActivationCostType Type,
    int Amount,
    bool IsPlayerChoiceVitaeOrWillpower = false,
    int PlayerChoiceWillpowerAmount = 1)
{
    /// <summary>Zero-cost power ("—", empty, or null).</summary>
    public static readonly ActivationCost None = new(ActivationCostType.None, 0);

    /// <summary>Gets a value indicating whether this power is free to activate.</summary>
    public bool IsNone => Type == ActivationCostType.None && !IsPlayerChoiceVitaeOrWillpower;

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

        const string vitaeOrWillpowerPattern = @"^(\d+)\s+vitae\s+or\s+(\d+)\s+willpower\s*$";
        Match orMatch = Regex.Match(
            trimmed,
            vitaeOrWillpowerPattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250));
        if (orMatch.Success)
        {
            int vitaeAmount = int.Parse(orMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            int willpowerAmount = int.Parse(orMatch.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            return new ActivationCost(
                ActivationCostType.Vitae,
                vitaeAmount,
                IsPlayerChoiceVitaeOrWillpower: true,
                PlayerChoiceWillpowerAmount: willpowerAmount);
        }

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
