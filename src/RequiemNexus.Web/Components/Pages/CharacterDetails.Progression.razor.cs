// Blazor partial: Beats and Experience adjustments on the character sheet (hub reload skip coordination).
using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private async Task AddBeat()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            CharacterProgressionSnapshotDto snap = await CharacterService.AddBeatAsync(_character.Id, _currentUserId);
            ApplyProgressionSnapshot(snap);
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
            CharacterProgressionSnapshotDto snap = await CharacterService.RemoveBeatAsync(_character.Id, _currentUserId);
            ApplyProgressionSnapshot(snap);
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
            CharacterProgressionSnapshotDto snap = await CharacterService.AddXPAsync(_character.Id, _currentUserId);
            ApplyProgressionSnapshot(snap);
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
            CharacterProgressionSnapshotDto snap = await CharacterService.RemoveXPAsync(_character.Id, _currentUserId);
            ApplyProgressionSnapshot(snap);
            if (_character.CampaignId.HasValue && xpBefore > 0)
            {
                _skipIncomingCharacterHubReloadCount++;
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private void ApplyProgressionSnapshot(CharacterProgressionSnapshotDto snap)
    {
        if (_character == null)
        {
            return;
        }

        _character.Beats = snap.Beats;
        _character.ExperiencePoints = snap.ExperiencePoints;
        _character.TotalExperiencePoints = snap.TotalExperiencePoints;
    }
}
