namespace RequiemNexus.Web.Components.Pages.Campaigns.CampaignDetailsParts;

/// <summary>
/// Form model for attaching an unassigned character to the campaign roster (player flow).
/// </summary>
public sealed class AddCharacterToCampaignModel
{
    /// <summary>Selected character id from the dropdown; 0 means none.</summary>
    public int CharacterId { get; set; }
}
