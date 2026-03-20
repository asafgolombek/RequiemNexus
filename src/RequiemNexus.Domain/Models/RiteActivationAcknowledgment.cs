namespace RequiemNexus.Domain.Models;

/// <summary>
/// Player confirmations for narrative sacrifices that the app does not inventory (Phase 9.5).
/// </summary>
/// <param name="AcknowledgePhysicalSacrament">Player confirms physical sacrament cost.</param>
/// <param name="AcknowledgeHeart">Player confirms heart or similar offering.</param>
/// <param name="AcknowledgeMaterialOffering">Player confirms material offering.</param>
/// <param name="AcknowledgeMaterialFocus">Player confirms required focus (not consumed).</param>
public record RiteActivationAcknowledgment(
    bool AcknowledgePhysicalSacrament = false,
    bool AcknowledgeHeart = false,
    bool AcknowledgeMaterialOffering = false,
    bool AcknowledgeMaterialFocus = false);
