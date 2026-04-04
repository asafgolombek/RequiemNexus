using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Models;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Helpers;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class StorytellerGlimpse
{
    private string? DegenerationModalMessage =>
        _degRollTargetId is int id && _degAlerts.TryGetValue(id, out (string Name, int Humanity, int Resolve) entry)
            ? $"{entry.Name} rolls degeneration ({DegenerationRollFormat.PoolHint(entry.Humanity, entry.Resolve)}). " +
              "Success (≥1): clear all stains, Humanity unchanged. " +
              "Failure: lose 1 Humanity, clear stains. Dramatic failure: also gain Guilty."
            : string.Empty;

    private void SyncDegenerationAlertsFromVitals()
    {
        foreach (CharacterVitalsDto v in _vitals)
        {
            if (v.HumanityStains >= v.Humanity)
            {
                _degAlerts[v.CharacterId] = (v.Name, v.Humanity, v.ResolveRating);
            }
            else
            {
                _degAlerts.Remove(v.CharacterId);
            }
        }
    }

    private void OpenDegenerationRollModal(int characterId)
    {
        _degRollTargetId = characterId;
        _degRollModalOpen = true;
    }

    private void CloseDegenerationRollModal()
    {
        _degRollModalOpen = false;
        _degRollTargetId = null;
    }

    private async Task ConfirmDegenerationRollAsync()
    {
        if (_degRollTargetId is not int cid || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        Result<DegenerationRollOutcome> result = await HumanityService.ExecuteDegenerationRollAsync(cid, _currentUserId);
        if (result.IsSuccess)
        {
            ToastService.Show("Degeneration", "Roll completed. See the dice feed for results.", ToastType.Success);
            CloseDegenerationRollModal();
            _degAlerts.Remove(cid);
            await LoadVitals();
        }
        else
        {
            ToastService.Show("Degeneration", result.Error ?? "Roll failed.", ToastType.Warning);
        }
    }

    private async Task RollRemorseForCharacterAsync(int characterId)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        Result<DegenerationRollOutcome> result = await TouchstoneService.RollRemorseAsync(characterId, _currentUserId);
        if (result.IsSuccess)
        {
            ToastService.Show("Remorse", "Roll completed. See the dice feed for results.", ToastType.Success);
            await LoadVitals();
        }
        else
        {
            ToastService.Show("Remorse", result.Error ?? "Roll failed.", ToastType.Warning);
        }
    }
}
