namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Client-supplied acknowledgments for narrative sacrifices when activating a rite (Phase 9.5).
/// </summary>
/// <param name="AcknowledgePhysicalSacrament">Sacrament or similar consumed offering acknowledged.</param>
/// <param name="AcknowledgeHeart">Heart (or equivalent) sacrifice acknowledged.</param>
/// <param name="AcknowledgeMaterialOffering">Material offering acknowledged.</param>
/// <param name="AcknowledgeMaterialFocus">Required focus (not consumed) acknowledged.</param>
/// <param name="ExtraVitae">Optional additional Vitae spent for Crúac-only dice bonus (V:tR 2e p. 153).</param>
/// <param name="TargetCharacterId">Optional Kindred target in the same chronicle for Blood Sympathy ritual bonus.</param>
public record BeginRiteActivationRequest(
    bool AcknowledgePhysicalSacrament = false,
    bool AcknowledgeHeart = false,
    bool AcknowledgeMaterialOffering = false,
    bool AcknowledgeMaterialFocus = false,
    int ExtraVitae = 0,
    int? TargetCharacterId = null);
