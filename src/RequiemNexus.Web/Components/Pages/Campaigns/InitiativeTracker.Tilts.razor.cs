using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private async Task LoadActiveTiltsAsync()
    {
        if (_encounter == null)
        {
            return;
        }

        _activeTilts = [];
        _tiltSelections = [];

        foreach (InitiativeEntry entry in _encounter.InitiativeEntries)
        {
            if (!entry.CharacterId.HasValue)
            {
                continue;
            }

            int charId = entry.CharacterId.Value;
            List<CharacterTilt> tilts = await ConditionService.GetActiveTiltsAsync(charId);
            _activeTilts[charId] = tilts;
            _tiltSelections.TryAdd(charId, TiltType.KnockedDown);
        }
    }

    private async Task ApplyTilt(int characterId)
    {
        if (_busy || !_tiltSelections.TryGetValue(characterId, out TiltType tiltType))
        {
            return;
        }

        _busy = true;
        try
        {
            await ConditionService.ApplyTiltAsync(characterId, tiltType, null, EncounterId, _currentUserId!);
            await LoadActiveTiltsAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RemoveTilt(int tiltId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await ConditionService.RemoveTiltAsync(tiltId, _currentUserId!);
            await LoadActiveTiltsAsync();
        }
        finally
        {
            _busy = false;
        }
    }
}
