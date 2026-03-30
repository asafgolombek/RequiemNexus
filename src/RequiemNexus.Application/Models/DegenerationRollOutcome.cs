namespace RequiemNexus.Application.Models;

/// <summary>
/// Result of a server-side degeneration check (VtR 2e): pool roll, stain clearing, optional Humanity loss, optional Guilty Condition.
/// </summary>
/// <param name="Successes">Successes on the rolled pool.</param>
/// <param name="HumanityUnchanged">True when the roll succeeded (at least one success); Humanity was not reduced.</param>
/// <param name="NewHumanity">Humanity rating after applying the outcome.</param>
/// <param name="GuiltyConditionApplied">True when a dramatic failure applied the Guilty Condition.</param>
public sealed record DegenerationRollOutcome(
    int Successes,
    bool HumanityUnchanged,
    int NewHumanity,
    bool GuiltyConditionApplied);
