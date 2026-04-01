namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Another Kindred in the same chronicle, selectable as an optional ritual target for Blood Sympathy pool bonuses.
/// </summary>
/// <param name="Id">Character identifier.</param>
/// <param name="Name">Display name.</param>
public sealed record CampaignKindredTargetDto(int Id, string Name);
