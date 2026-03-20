namespace RequiemNexus.Domain.Models;

/// <summary>
/// Trackable resources on a character at activation time, used for pure validation in the Domain.
/// </summary>
/// <param name="CurrentVitae">Current Vitae in the pool.</param>
/// <param name="CurrentWillpower">Current Willpower.</param>
/// <param name="HumanityStains">Current stain count before paying costs.</param>
public record RiteActivationResourceSnapshot(
    int CurrentVitae,
    int CurrentWillpower,
    int HumanityStains);
