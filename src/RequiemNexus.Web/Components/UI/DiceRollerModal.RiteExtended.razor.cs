using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.UI;

#pragma warning disable SA1201 // Partial mirrors feature grouping
#pragma warning disable SA1202

public partial class DiceRollerModal
{
    private Task OnAddRiteDisciplineDotsToPotencyChanged(bool value)
    {
        _addRiteDisciplineDotsToPotency = value;
        return Task.CompletedTask;
    }

    private bool IsRiteExtendedMode =>
        FixedDicePool.HasValue
        && RiteExtendedMaxRolls.HasValue
        && RiteExtendedTargetSuccesses.HasValue;

    private int RiteRollsRemaining =>
        !RiteExtendedMaxRolls.HasValue ? 0 : Math.Max(0, RiteExtendedMaxRolls.Value - _riteRollsUsed);

    private bool ShowRiteCompletedBanner =>
        IsRiteExtendedMode
        && _riteAccumulatedSuccesses >= RiteExtendedTargetSuccesses!.Value;

    private bool ShowRiteFailedNoRollsBanner =>
        IsRiteExtendedMode
        && _lastResultDto != null
        && !_isRolling
        && _riteRollsUsed >= RiteExtendedMaxRolls!.Value
        && _riteAccumulatedSuccesses < RiteExtendedTargetSuccesses!.Value;

    private bool RiteRollBlocked =>
        IsRiteExtendedMode
        && (
            _riteFailureNeedsChoice
            || _riteAccumulatedSuccesses >= RiteExtendedTargetSuccesses!.Value
            || _riteRollsUsed >= RiteExtendedMaxRolls!.Value
            || RiteExtendedMaxRolls!.Value <= 0);

    private bool RiteRollButtonDisabled => _isRolling || RiteRollBlocked;

    private async Task ContinueAfterRiteFailureAsync()
    {
        if (Character != null && RiteOutcomeTradition.HasValue)
        {
            string? userId = await TryGetCurrentUserIdAsync();
            if (userId != null)
            {
                try
                {
                    await RiteRollOutcomeService.ApplyRiteRollOutcomeAsync(
                        Character.Id,
                        userId,
                        RiteOutcomeTradition.Value,
                        RiteRollOutcomeTrigger.ContinueAfterZeroSuccesses);
                }
                catch (Exception ex)
                {
                    ToastService.Show("Rite", ex.Message, ToastType.Error);
                    return;
                }
            }
        }

        _riteFailureNeedsChoice = false;
    }

    private async Task AbandonRite()
    {
        await Close();
    }

    private bool ApplyRiteExtendedRollAccounting(DiceRollResultDto rollResult)
    {
        if (!IsRiteExtendedMode || !_riteRollInFlight)
        {
            return false;
        }

        if (!rollResult.PoolDescription.Contains(_riteExtendedDescriptionMarker, StringComparison.OrdinalIgnoreCase))
        {
            _riteRollInFlight = false;
            return false;
        }

        int max = RiteExtendedMaxRolls!.Value;
        int target = RiteExtendedTargetSuccesses!.Value;

        _riteRollsUsed++;
        _riteAccumulatedSuccesses += rollResult.Successes;
        _riteRollInFlight = false;

        if (rollResult.IsExceptionalSuccess)
        {
            _riteHadExceptionalSuccess = true;
        }

        _riteFailureNeedsChoice = rollResult.Successes == 0
            && _riteRollsUsed < max
            && _riteAccumulatedSuccesses < target;
        return true;
    }
}
