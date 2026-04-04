using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
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
    private string GetShareUrl() => $"{NavigationManager.BaseUri}rolls/{_shareSlug}";

    private async Task CopyShareUrl()
    {
        if (_shareSlug == null || _copying)
        {
            return;
        }

        var url = GetShareUrl();
        _copying = true;
        try
        {
            await JS.InvokeVoidAsync("copyToClipboard", url);
            ToastService.Show("Copied", "Link copied to clipboard.", ToastType.Success, 2000);
        }
        finally
        {
            _copying = false;
        }
    }

    private async Task ShareRoll()
    {
        if (_lastResultDto == null || _sharing)
        {
            return;
        }

        _sharing = true;
        try
        {
            var authState = await AuthProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                var description = string.IsNullOrEmpty(_associatedTraitName)
                    ? TraitName
                    : $"{TraitName} + {_associatedTraitName}";

                if (_modifier != 0)
                {
                    description += $" ({(_modifier > 0 ? "+" : string.Empty)}{_modifier})";
                }

                _shareSlug = await PublicRollService.ShareRollAsync(userId, Character?.CampaignId, description, _lastResultDto);
            }
        }
        finally
        {
            _sharing = false;
        }
    }

    private async Task RollDice()
    {
        if (RiteRollButtonDisabled)
        {
            return;
        }

        if (IsRiteExtendedMode && RiteExtendedMaxRolls.GetValueOrDefault() <= 0)
        {
            ToastService.Show("Rite", "This rite has no unmodified pool for extended rolls.", ToastType.Warning);
            return;
        }

        _isRolling = true;
        _showBurst = false;
        _shakeClass = string.Empty;
        _shareSlug = null;

        string description;
        if (IsRiteExtendedMode)
        {
            int nextRoll = _riteRollsUsed + 1;
            description = $"{TraitName} — {_riteExtendedDescriptionMarker} (roll {nextRoll} of {RiteExtendedMaxRolls!.Value})";
            _riteRollInFlight = true;
        }
        else
        {
            description = string.IsNullOrEmpty(_associatedTraitName)
                ? TraitName
                : $"{TraitName} + {_associatedTraitName}";

            if (_modifier != 0)
            {
                description += $" ({(_modifier > 0 ? "+" : string.Empty)}{_modifier})";
            }
        }

        bool chanceDie = IsChanceDiePool;
        bool tenAgain = chanceDie ? false : _tenAgain;
        bool nineAgain = chanceDie ? false : _nineAgain;
        bool eightAgain = chanceDie ? false : _eightAgain;
        bool isRote = chanceDie ? false : _isRote;

        try
        {
            if (SessionClient.IsConnected && Character?.CampaignId != null)
            {
                await SessionClient.RollDiceAsync(
                    Character.CampaignId.Value,
                    Character.Id,
                    TotalPool,
                    description,
                    tenAgain,
                    nineAgain,
                    eightAgain,
                    isRote);
            }
            else
            {
                var result = DiceService.Roll(TotalPool, tenAgain, nineAgain, eightAgain, isRote, _seed);

                var userName = Character?.Name ?? "Player";
                var rollDto = new DiceRollResultDto(
                    userName,
                    "offline",
                    Character?.Id,
                    description,
                    result.Successes,
                    result.IsExceptionalSuccess,
                    result.IsDramaticFailure,
                    result.DiceRolled,
                    DateTimeOffset.UtcNow,
                    result.IsChanceDie);

                await ProcessRollResult(rollDto);
            }
        }
        catch (Exception)
        {
            _isRolling = false;
            _riteRollInFlight = false;
            throw;
        }
    }

    private void HandleDiceRollReceived(DiceRollResultDto roll)
    {
        if (!IsVisible)
        {
            return;
        }

        if (Character != null && roll.CharacterId.HasValue && roll.CharacterId != Character.Id)
        {
            return;
        }

        if (IsRiteExtendedMode && _riteRollInFlight
            && !roll.PoolDescription.Contains(_riteExtendedDescriptionMarker, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = InvokeAsync(() => ProcessRollResult(roll));
    }

    private async Task ProcessRollResult(DiceRollResultDto result)
    {
        _lastResultDto = result;

        await Task.Delay(800);

        _isRolling = false;

        if (_lastResultDto.IsDramaticFailure)
        {
            _shakeClass = "shake";
        }

        if (_lastResultDto.IsExceptionalSuccess)
        {
            _showBurst = true;
        }

        bool riteRollCounted = ApplyRiteExtendedRollAccounting(result);

        if (riteRollCounted && Character != null && RiteOutcomeTradition.HasValue)
        {
            string? userId = await TryGetCurrentUserIdAsync();
            if (userId != null)
            {
                try
                {
                    if (result.IsDramaticFailure)
                    {
                        await RiteRollOutcomeService.ApplyRiteRollOutcomeAsync(
                            Character.Id,
                            userId,
                            RiteOutcomeTradition.Value,
                            RiteRollOutcomeTrigger.DramaticFailure);
                    }
                    else if (result.IsExceptionalSuccess)
                    {
                        await RiteRollOutcomeService.ApplyRiteRollOutcomeAsync(
                            Character.Id,
                            userId,
                            RiteOutcomeTradition.Value,
                            RiteRollOutcomeTrigger.ExceptionalSuccess);
                    }
                }
                catch (Exception ex)
                {
                    ToastService.Show("Rite", ex.Message, ToastType.Error);
                }
            }
        }

        StateHasChanged();

        if (_showBurst)
        {
            await Task.Delay(1500);
            _showBurst = false;
            StateHasChanged();
        }
    }

    private async Task<string?> TryGetCurrentUserIdAsync()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
