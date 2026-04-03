// Blazor partial: discipline activation, bloodline, and covenant modals for CharacterDetails.
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    private async Task OpenDisciplinePowerActivateModal(DisciplinePower power)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            _disciplineActivatePower = power;
            _disciplineActivatePool = await DisciplineActivationService.ResolveActivationPoolAsync(
                _character.Id,
                power.Id,
                _currentUserId);
            _isDisciplineActivateModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Discipline", ex.Message, ToastType.Error);
        }
    }

    private async Task HandleDisciplineActivateConfirmedAsync(DisciplineActivationResourceChoice? resourceChoice)
    {
        if (_character == null || _disciplineActivatePower == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _isDisciplineActivateModalOpen = false;
        int characterId = _character.Id;
        string userId = _currentUserId;
        DisciplinePower power = _disciplineActivatePower;
        try
        {
            int dice = await DisciplineActivationService.ActivatePowerAsync(
                characterId,
                power.Id,
                userId,
                resourceChoice);
            _character = await CharacterService.ReloadCharacterAsync(characterId, userId);
            await ResolveDisciplinePowerPoolsAsync();
            _rollerTraitName = power.Name;
            _rollerBaseDice = dice;
            _rollerFixedDicePool = null;
            ClearRiteExtendedRollerContext();
            _isRollerOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Discipline", ex.Message, ToastType.Error);
            _character = await CharacterService.ReloadCharacterAsync(characterId, userId);
        }
    }

    private void HandleApplyBloodlineModalClosed(bool isOpen)
    {
        _isApplyBloodlineModalOpen = isOpen;
    }

    /// <summary>Returns whether the given modal-open handler is currently loading prerequisite data.</summary>
    /// <param name="openerName">Use <c>nameof(OpenLearnRiteModal)</c> (or sibling openers) from markup.</param>
    private bool IsOpeningModal(string openerName) =>
        string.Equals(_pendingModal, openerName, StringComparison.Ordinal);

    private async Task OpenApplyBloodlineModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _pendingModal = nameof(OpenApplyBloodlineModal);
        await InvokeAsync(StateHasChanged);
        try
        {
            _eligibleBloodlines = await BloodlineService.GetEligibleBloodlinesAsync(_character.Id, _currentUserId);
            if (_eligibleBloodlines.Count == 0)
            {
                ToastService.Show("No eligible bloodlines", "No eligible bloodlines for your clan.", ToastType.Info);
                return;
            }

            _isApplyBloodlineModalOpen = true;
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

    private async Task HandleApplyBloodlineRequested(int bloodlineDefinitionId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await BloodlineService.ApplyForBloodlineAsync(_character.Id, bloodlineDefinitionId, _currentUserId);
            ToastService.Show("Application submitted", "Bloodline application submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task RemoveBloodline(int characterBloodlineId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId) || _removingBloodline)
        {
            return;
        }

        _removingBloodline = true;
        try
        {
            await BloodlineService.RemoveBloodlineAsync(characterBloodlineId, _currentUserId);
            ToastService.Show("Bloodline removed", "The bloodline has been removed from your character.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _removingBloodline = false;
        }
    }

    private void HandleApplyCovenantModalClosed(bool isOpen)
    {
        _isApplyCovenantModalOpen = isOpen;
    }

    private async Task OpenApplyCovenantModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _pendingModal = nameof(OpenApplyCovenantModal);
        await InvokeAsync(StateHasChanged);
        try
        {
            _eligibleCovenants = await CovenantService.GetEligibleCovenantsAsync(_character.Id, _currentUserId);
            if (_eligibleCovenants.Count == 0)
            {
                ToastService.Show("No eligible covenants", "Join a campaign first, or you may already have a pending application.", ToastType.Info);
                return;
            }

            _isApplyCovenantModalOpen = true;
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

    private async Task HandleApplyCovenantRequested(int covenantDefinitionId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await CovenantService.ApplyForCovenantAsync(_character.Id, covenantDefinitionId, _currentUserId);
            ToastService.Show("Application submitted", "Covenant application submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task RequestLeaveCovenant()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId) || _requestingLeave)
        {
            return;
        }

        _requestingLeave = true;
        try
        {
            await CovenantService.RequestLeaveCovenantAsync(_character.Id, _currentUserId);
            ToastService.Show("Leave requested", "Your request to leave the covenant has been submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _requestingLeave = false;
        }
    }
}
