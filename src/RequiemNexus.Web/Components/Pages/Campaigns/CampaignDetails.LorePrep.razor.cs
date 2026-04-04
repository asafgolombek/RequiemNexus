namespace RequiemNexus.Web.Components.Pages.Campaigns;

/// <summary>
/// Partial: campaign lore entries and storyteller session prep notes for <see cref="CampaignDetails"/>.
/// </summary>
public partial class CampaignDetails
{
    private async Task CreateLore()
    {
        if (string.IsNullOrWhiteSpace(_newLoreTitle) || _campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        await CampaignService.CreateLoreAsync(_campaign.Id, _newLoreTitle.Trim(), _newLoreBody.Trim(), _currentUserId);
        _newLoreTitle = string.Empty;
        _newLoreBody = string.Empty;
        _showAddLoreForm = false;
        _loreEntries = await CampaignService.GetLoreAsync(_campaign.Id);
    }

    private void DiscardAddLore()
    {
        _newLoreTitle = string.Empty;
        _newLoreBody = string.Empty;
        _showAddLoreForm = false;
    }

    private async Task DeleteLore(int loreId)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        await CampaignService.DeleteLoreAsync(loreId, _currentUserId);
        _loreEntries = await CampaignService.GetLoreAsync(_campaign!.Id);
    }

    private async Task CreateSessionPrepNote()
    {
        if (string.IsNullOrWhiteSpace(_newPrepTitle) || _campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        await CampaignService.CreateSessionPrepNoteAsync(_campaign.Id, _newPrepTitle.Trim(), _newPrepBody.Trim(), _currentUserId);
        _newPrepTitle = string.Empty;
        _newPrepBody = string.Empty;
        _showAddPrepForm = false;
        _sessionPrepNotes = await CampaignService.GetSessionPrepNotesAsync(_campaign.Id, _currentUserId);
    }

    private void DiscardAddPrep()
    {
        _newPrepTitle = string.Empty;
        _newPrepBody = string.Empty;
        _showAddPrepForm = false;
    }

    private async Task DeleteSessionPrepNote(int noteId)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        await CampaignService.DeleteSessionPrepNoteAsync(noteId, _currentUserId);
        _sessionPrepNotes = await CampaignService.GetSessionPrepNotesAsync(_campaign!.Id, _currentUserId);
    }
}
