using RequiemNexus.Data.Models;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private void CancelDefeatConfirm() => _defeatConfirmEntryId = null;

    private async Task ConfirmDefeatNpc(int entryId)
    {
        _defeatConfirmEntryId = null;
        await RemoveEntry(entryId);
    }

    private async Task AdvanceTurn()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        _actionFeedback = string.Empty;
        try
        {
            await EncounterService.AdvanceTurnAsync(EncounterId, _currentUserId!);
            await LoadEncounter();

            if (SortedEntries.All(i => !i.HasActed))
            {
                _actionFeedback = $"Round {_encounter?.CurrentRound ?? 1}";
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task HoldTurn()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.HoldActionAsync(EncounterId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ReleaseHeld(int entryId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.ReleaseHeldActionAsync(EncounterId, entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ResolveEncounter()
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.ResolveEncounterAsync(EncounterId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RemoveEntry(int entryId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterParticipantService.RemoveEntryAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private void OnDragStart(InitiativeEntry item) => InitiativeTrackerDragState.SetDraggedItem(item);

    private async Task OnDrop(InitiativeEntry target)
    {
        InitiativeEntry? dragged = InitiativeTrackerDragState.DraggedItem;
        if (dragged is null || dragged == target || _encounter == null)
        {
            return;
        }

        List<InitiativeEntry> list = _encounter.InitiativeEntries.OrderBy(i => i.Order).ToList();
        int oldIdx = list.IndexOf(dragged);
        int newIdx = list.IndexOf(target);

        list.RemoveAt(oldIdx);
        list.Insert(newIdx, dragged);

        List<int> orderIds = list.Select(e => e.Id).ToList();
        InitiativeTrackerDragState.ClearDrag();

        _busy = true;
        try
        {
            await EncounterService.ReorderInitiativeAsync(EncounterId, orderIds, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }
}
