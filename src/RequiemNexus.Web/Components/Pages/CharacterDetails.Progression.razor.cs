// Blazor partial: Beats and Experience adjustments on the character sheet (hub reload skip coordination).
namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private async Task AddBeat()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CharacterService.AddBeatAsync(_character.Id, _currentUserId);
            if (_character.CampaignId.HasValue)
            {
                _skipIncomingCharacterHubReloadCount++;
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RemoveBeat()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            int beatsBefore = _character.Beats;
            await CharacterService.RemoveBeatAsync(_character.Id, _currentUserId);
            if (_character.CampaignId.HasValue && beatsBefore > 0)
            {
                _skipIncomingCharacterHubReloadCount++;
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task AddXP()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CharacterService.AddXPAsync(_character.Id, _currentUserId);
            if (_character.CampaignId.HasValue)
            {
                _skipIncomingCharacterHubReloadCount++;
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task RemoveXP()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            int xpBefore = _character.ExperiencePoints;
            await CharacterService.RemoveXPAsync(_character.Id, _currentUserId);
            if (_character.CampaignId.HasValue && xpBefore > 0)
            {
                _skipIncomingCharacterHubReloadCount++;
            }

            await InvokeAsync(StateHasChanged);
        }
    }
}
