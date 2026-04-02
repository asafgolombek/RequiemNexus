namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Player choices before applying rite costs and opening the dice roller (Crúac extra Vitae, optional Blood Sympathy target, narrative acknowledgments).
/// </summary>
/// <param name="ExtraVitae">Additional Vitae for Crúac dice bonus only; ignored for other traditions.</param>
/// <param name="TargetCharacterId">Optional Kindred in the same chronicle for Blood Sympathy pool bonus.</param>
/// <param name="AcknowledgePhysicalSacrament">Player confirms the physical sacrament is ready at the table (Theban and other rites that require it).</param>
/// <param name="AcknowledgeHeart">Player confirms heart or equivalent narrative sacrifice.</param>
/// <param name="AcknowledgeMaterialOffering">Player confirms material offering.</param>
/// <param name="AcknowledgeMaterialFocus">Player confirms material focus.</param>
public sealed record RiteActivationPrepResult(
    int ExtraVitae,
    int? TargetCharacterId,
    bool AcknowledgePhysicalSacrament = false,
    bool AcknowledgeHeart = false,
    bool AcknowledgeMaterialOffering = false,
    bool AcknowledgeMaterialFocus = false);
