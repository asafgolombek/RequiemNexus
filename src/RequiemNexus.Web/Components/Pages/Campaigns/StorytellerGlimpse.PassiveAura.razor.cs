using RequiemNexus.Application.DTOs;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class StorytellerGlimpse
{
    private Task OnPassiveAuraCharacterAChangedAsync(int value)
    {
        _passiveAuraCharacterA = value;
        return Task.CompletedTask;
    }

    private Task OnPassiveAuraCharacterBChangedAsync(int value)
    {
        _passiveAuraCharacterB = value;
        return Task.CompletedTask;
    }

    private async Task TriggerPassivePredatoryAuraAsync()
    {
        if (_passiveAuraBusy || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _passiveAuraBusy = true;
        _passiveAuraMessage = string.Empty;
        _passiveAuraError = false;
        try
        {
            Result<PredatoryAuraContestResultDto?> result = await PredatoryAuraService.ResolvePassiveContestAsync(
                Id,
                _passiveAuraCharacterA,
                _passiveAuraCharacterB,
                _currentUserId,
                encounterId: null);

            if (!result.IsSuccess)
            {
                _passiveAuraError = true;
                _passiveAuraMessage = result.Error ?? "Contest failed.";
                return;
            }

            if (result.Value is null)
            {
                _passiveAuraMessage = "Contest skipped (already resolved for this encounter pair).";
                return;
            }

            PredatoryAuraContestResultDto dto = result.Value;
            _passiveAuraMessage = $"{dto.AttackerName} vs {dto.DefenderName} — {dto.Outcome}.";
            ToastService.Show("Passive Predatory Aura", "Contest resolved — see dice feed.", ToastType.Success);
            await LoadVitals();
        }
        finally
        {
            _passiveAuraBusy = false;
        }
    }
}
