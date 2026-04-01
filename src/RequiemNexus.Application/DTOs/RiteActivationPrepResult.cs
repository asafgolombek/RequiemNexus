namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Player choices before applying rite costs and opening the dice roller (Crúac extra Vitae, optional Blood Sympathy target).
/// </summary>
/// <param name="ExtraVitae">Additional Vitae for Crúac dice bonus only; ignored for other traditions.</param>
/// <param name="TargetCharacterId">Optional Kindred in the same chronicle for Blood Sympathy pool bonus.</param>
public sealed record RiteActivationPrepResult(int ExtraVitae, int? TargetCharacterId);
