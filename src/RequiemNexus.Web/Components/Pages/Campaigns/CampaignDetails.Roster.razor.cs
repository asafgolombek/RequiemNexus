using RequiemNexus.Application.DTOs;
using RequiemNexus.Web.Enums;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

/// <summary>
/// Partial: roster tab (players, perception), character enrollment, and danger-zone actions for <see cref="CampaignDetails"/>.
/// </summary>
public partial class CampaignDetails
{
    private void NavigateToCharacter(int characterId, bool isOwner)
    {
        if (isOwner)
        {
            NavigationManager.NavigateTo($"/character/{characterId}");
        }
        else
        {
            NavigationManager.NavigateTo($"/campaigns/{Id}/characters/{characterId}");
        }
    }

    private void ToggleAddCharacter()
    {
        _showAddCharacter = !_showAddCharacter;
        _addModel.CharacterId = 0;
    }

    private async Task AddCharacterSubmit()
    {
        if (_addModel.CharacterId > 0 && _campaign != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CampaignService.AddCharacterToCampaignAsync(_campaign.Id, _addModel.CharacterId, _currentUserId);
            await LoadData();
            _showAddCharacter = false;
        }
    }

    private void CancelConfirm()
    {
        _showConfirmDelete = false;
        _showConfirmLeave = false;
        _confirmRemoveCharacterId = 0;
    }

    private void AskRemoveCharacter(int characterId)
    {
        _confirmRemoveCharacterId = characterId;
    }

    private async Task ConfirmRemoveCharacter(int characterId)
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await CampaignService.RemoveCharacterFromCampaignAsync(_campaign.Id, characterId, _currentUserId);
            _confirmRemoveCharacterId = 0;
            await LoadData();
        }
        catch (Exception ex)
        {
            ToastService.Show("Campaign", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ConfirmLeaveCampaign()
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await CampaignService.LeaveCampaignAsync(_campaign.Id, _currentUserId);
            _showConfirmLeave = false;
            await LoadData();
        }
        catch (Exception ex)
        {
            ToastService.Show("Campaign", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ConfirmDeleteCampaign()
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await CampaignService.DeleteCampaignAsync(_campaign.Id, _currentUserId);
            NavigationManager.NavigateTo("/campaigns");
        }
        catch (Exception ex)
        {
            ToastService.Show("Campaign", ex.Message, ToastType.Error);
            _busy = false;
        }
    }

    private void TogglePerception(int characterId)
    {
        _perceptionOpenId = _perceptionOpenId == characterId ? null : characterId;
    }

    private Task OnTogglePerceptionAsync(int characterId)
    {
        TogglePerception(characterId);
        return Task.CompletedTask;
    }

    private Task OnAskRemoveCharacterAsync(int characterId)
    {
        AskRemoveCharacter(characterId);
        return Task.CompletedTask;
    }

    private async Task RollPerception(int characterId)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _perceptionBusy = true;
        StateHasChanged();
        try
        {
            PerceptionRollResultDto result = await PerceptionRollService.RollPerceptionAsync(
                characterId,
                PerceptionUseAwareness,
                PerceptionPenalty,
                _currentUserId);
            string dice = string.Join(", ", result.DiceRolled);
            _perceptionResults[characterId] =
                $"{result.PoolDescription}: [{dice}] → {result.Successes} successes" +
                (result.IsExceptionalSuccess ? " (exceptional)" : string.Empty) +
                (result.IsDramaticFailure ? " (dramatic failure)" : string.Empty);
        }
        catch (Exception ex)
        {
            _perceptionResults[characterId] = ex.Message;
        }
        finally
        {
            _perceptionBusy = false;
            StateHasChanged();
        }
    }
}
