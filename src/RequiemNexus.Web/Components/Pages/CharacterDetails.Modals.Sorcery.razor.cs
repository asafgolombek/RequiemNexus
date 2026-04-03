// Blazor partial: sorcery rite learning, Chosen Mystery, and Ordo coil purchase modals for CharacterDetails.
using RequiemNexus.Application.Contracts;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private void HandleApplyLearnRiteModalClosed(bool isOpen)
    {
        _isApplyLearnRiteModalOpen = isOpen;
    }

    private async Task OpenLearnRiteModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _pendingModal = nameof(OpenLearnRiteModal);
        await InvokeAsync(StateHasChanged);
        try
        {
            _eligibleRites = await SorceryService.GetEligibleRitesAsync(_character.Id, _currentUserId);
            if (_eligibleRites.Count == 0)
            {
                ToastService.Show("No eligible rites", "You need more Crúac or Theban Sorcery dots, or you've learned all available rites.", ToastType.Info);
                return;
            }

            _isApplyLearnRiteModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _pendingModal = null;
        }
    }

    private async Task HandleApplyLearnRiteRequested(int sorceryRiteDefinitionId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await SorceryService.RequestLearnRiteAsync(_character.Id, sorceryRiteDefinitionId, _currentUserId);
            ToastService.Show("Request submitted", "Rite learning request submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task OpenChosenMysteryModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _pendingModal = nameof(OpenChosenMysteryModal);
        await InvokeAsync(StateHasChanged);
        try
        {
            _eligibleScales = await CoilService.GetScalesAsync();
            if (_eligibleScales.Count == 0)
            {
                ToastService.Show("No scales found", "No Scale definitions found in the database.", ToastType.Info);
                return;
            }

            _isChosenMysteryModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _pendingModal = null;
        }
    }

    private async Task HandleChosenMysteryRequested(int scaleId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await CoilService.RequestChosenMysteryAsync(_character.Id, scaleId, _currentUserId);
            ToastService.Show("Request submitted", "Chosen Mystery request submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _isChosenMysteryModalOpen = false;
        }
    }

    private async Task OpenLearnCoilModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _pendingModal = nameof(OpenLearnCoilModal);
        await InvokeAsync(StateHasChanged);
        try
        {
            _eligibleCoils = await CoilService.GetEligibleCoilsAsync(_character.Id, _currentUserId);
            if (_eligibleCoils.Count == 0)
            {
                ToastService.Show("No eligible coils", "No eligible Coils to purchase. Check prerequisites and Ordo Status.", ToastType.Info);
                return;
            }

            _isLearnCoilModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _pendingModal = null;
        }
    }

    private async Task HandleLearnCoilRequested(int coilDefinitionId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await CoilService.RequestLearnCoilAsync(_character.Id, coilDefinitionId, _currentUserId);
            ToastService.Show("Request submitted", "Coil purchase request submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _isLearnCoilModalOpen = false;
        }
    }
}
