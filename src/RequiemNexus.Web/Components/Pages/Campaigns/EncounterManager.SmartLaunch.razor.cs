using Microsoft.AspNetCore.Components;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

/// <summary>
/// Partial: smart-launch player selection and encounter lifecycle (pause / resume / resolve).
/// </summary>
public partial class EncounterManager
{
    private bool GetSmartCheck(int characterId) =>
        _smartLaunchSelection.GetValueOrDefault(characterId, true);

    private void SetSmartCheck(int characterId, bool value) => _smartLaunchSelection[characterId] = value;

    private Task OnSmartLaunchSelectionChangedAsync((int CharacterId, bool Selected) args)
    {
        SetSmartCheck(args.CharacterId, args.Selected);
        return Task.CompletedTask;
    }

    private Task CancelSmartLaunchAsync()
    {
        CancelSmartLaunch();
        return Task.CompletedTask;
    }

    private Task OpenSmartLaunchPrepAsync(int encounterId)
    {
        OpenSmartLaunch(encounterId, prepStart: true);
        return Task.CompletedTask;
    }

    private Task OpenSmartLaunchForActiveAsync(int encounterId)
    {
        OpenSmartLaunch(encounterId, prepStart: false);
        return Task.CompletedTask;
    }

    private async Task ResolveEncounter(int encounterId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.ResolveEncounterAsync(encounterId, _currentUserId!);
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }

    private void OpenSmartLaunch(int encounterId, bool prepStart)
    {
        _smartLaunchEncounterId = encounterId;
        _smartLaunchIsPrepStart = prepStart;
        _createError = string.Empty;
        foreach (Character ch in _campaignCharacters)
        {
            _smartLaunchSelection[ch.Id] = true;
        }
    }

    private void CancelSmartLaunch()
    {
        _smartLaunchEncounterId = null;
        _smartLaunchIsPrepStart = false;
    }

    private async Task ConfirmSmartLaunch()
    {
        if (!_smartLaunchEncounterId.HasValue || _busy)
        {
            return;
        }

        bool startedFromPrep = _smartLaunchIsPrepStart;
        int encounterId = _smartLaunchEncounterId.Value;

        List<int> ids = _campaignCharacters
            .Where(c => _smartLaunchSelection.GetValueOrDefault(c.Id, true))
            .Select(c => c.Id)
            .ToList();

        _busy = true;
        _createError = string.Empty;
        try
        {
            if (startedFromPrep)
            {
                await EncounterService.LaunchEncounterAsync(encounterId, _currentUserId!);
            }

            await EncounterParticipantService.BulkAddOnlinePlayersAsync(encounterId, ids, _currentUserId!);

            CancelSmartLaunch();
            await LoadEncounters();

            if (startedFromPrep)
            {
                NavigationManager.NavigateTo($"/campaigns/{Id}/encounter/{encounterId}", forceLoad: true);
            }
        }
        catch (Exception ex)
        {
            await LoadEncounters();
            ToastService.Show("Encounter", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task PauseEncounter(int encounterId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.PauseEncounterAsync(encounterId, _currentUserId!);
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ResumeEncounter(int encounterId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.ResumeEncounterAsync(encounterId, _currentUserId!);
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }
}
