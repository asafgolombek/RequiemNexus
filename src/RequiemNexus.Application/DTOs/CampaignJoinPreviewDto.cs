namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Minimal campaign info returned after a valid join invite is presented (no roster).
/// </summary>
/// <param name="CampaignId">Chronicle identifier.</param>
/// <param name="Name">Display name of the campaign.</param>
public sealed record CampaignJoinPreviewDto(int CampaignId, string Name);
