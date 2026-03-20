namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Client-supplied acknowledgments for narrative sacrifices when activating a rite (Phase 9.5).
/// </summary>
/// <param name="AcknowledgePhysicalSacrament">Sacrament or similar consumed offering acknowledged.</param>
/// <param name="AcknowledgeHeart">Heart (or equivalent) sacrifice acknowledged.</param>
/// <param name="AcknowledgeMaterialOffering">Material offering acknowledged.</param>
/// <param name="AcknowledgeMaterialFocus">Required focus (not consumed) acknowledged.</param>
public record BeginRiteActivationRequest(
    bool AcknowledgePhysicalSacrament = false,
    bool AcknowledgeHeart = false,
    bool AcknowledgeMaterialOffering = false,
    bool AcknowledgeMaterialFocus = false);
