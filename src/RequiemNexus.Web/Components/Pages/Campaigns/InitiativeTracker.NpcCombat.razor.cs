using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private async Task OnNpcHealthTrackChangedAsync(int entryId, string track)
    {
        if (_busy || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        _actionFeedback = string.Empty;
        try
        {
            await NpcCombatService.SetNpcHealthDamageAsync(entryId, track, _currentUserId);
            await LoadEncounter(showFullPageSpinner: false);
        }
        catch (Exception ex)
        {
            _actionFeedback = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task NpcSpendWill(int entryId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await NpcCombatService.SpendNpcWillpowerAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task NpcRestoreWill(int entryId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await NpcCombatService.RestoreNpcWillpowerAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task NpcSpendVitae(int entryId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await NpcCombatService.SpendNpcVitaeAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task NpcRestoreVitae(int entryId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await NpcCombatService.RestoreNpcVitaeAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private void ToggleNpcRoll(int entryId)
    {
        _npcRollEntryId = _npcRollEntryId == entryId ? null : entryId;
    }

    private Task CloseNpcRollPanelAsync()
    {
        _npcRollEntryId = null;
        return Task.CompletedTask;
    }

    private async Task ToggleNpcReveal(InitiativeEntry entry)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await NpcCombatService.SetNpcEntryRevealAsync(
                entry.Id, !entry.IsRevealed, entry.MaskedDisplayName, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }
}
