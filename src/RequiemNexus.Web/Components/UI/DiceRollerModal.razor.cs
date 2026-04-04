using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.UI;

#pragma warning disable SA1201 // Order mirrors original @code / inject layout
#pragma warning disable SA1202
#pragma warning disable SA1214 // Readonly array fields grouped with related state

/// <summary>Code-behind for the shared dice roller modal (standard pools, extended rites, hub/offline rolls).</summary>
public partial class DiceRollerModal : IDisposable
{
    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback<bool> IsVisibleChanged { get; set; }

    [Parameter]
    public string TraitName { get; set; } = string.Empty;

    [Parameter]
    public int BaseDice { get; set; } = 1;

    [Parameter]
    public Character? Character { get; set; }

    /// <summary>When set, pool size is fixed (e.g. devotion) and associated trait UI is hidden.</summary>
    [Parameter]
    public int? FixedDicePool { get; set; }

    /// <summary>Optional blood rite extended-action cap (unmodified pool). Requires <see cref="FixedDicePool"/> and <see cref="RiteExtendedTargetSuccesses"/>.</summary>
    [Parameter]
    public int? RiteExtendedMaxRolls { get; set; }

    /// <summary>Successes required to complete the rite (shown in the roller).</summary>
    [Parameter]
    public int? RiteExtendedTargetSuccesses { get; set; }

    /// <summary>Minutes per extended roll (30 or 15 per V:tR 2e).</summary>
    [Parameter]
    public int? RiteExtendedMinutesPerRoll { get; set; }

    /// <summary>Tradition for ritual outcome Conditions (Crúac / Theban / Necromancy). Set with extended rite parameters.</summary>
    [Parameter]
    public SorceryType? RiteOutcomeTradition { get; set; }

    /// <summary>Ritual Discipline dots (from begin activation) for optional Potency on exceptional success.</summary>
    [Parameter]
    public int? RiteExtendedRitualDisciplineDots { get; set; }

    private const string _riteExtendedDescriptionMarker = "extended rite";

    private string _associatedTraitName = string.Empty;
    private int _modifier;
    private bool _tenAgain = true;
    private bool _nineAgain;
    private bool _eightAgain;
    private bool _isRote;
    private int? _seed;
    private DiceRollResultDto? _lastResultDto;
    private bool _showTools;

    private readonly string _successCountId = $"success-count-{Guid.NewGuid():N}";
    private string _shakeClass = string.Empty;
    private bool _showBurst;
    private bool _isRolling;
    private bool _sharing;
    private bool _copying;
    private string? _shareSlug;

    private int _riteRollsUsed;
    private int _riteAccumulatedSuccesses;
    private bool _riteFailureNeedsChoice;
    private bool _riteRollInFlight;
    private bool _riteHadExceptionalSuccess;
    private bool _addRiteDisciplineDotsToPotency;
    private bool _rollerWasVisible;

    private IDisposable? _diceRollSubscription;

    private Task OnAddRiteDisciplineDotsToPotencyChanged(bool value)
    {
        _addRiteDisciplineDotsToPotency = value;
        return Task.CompletedTask;
    }

    private readonly string[] _attributes = TraitMetadata.AllAttributes.Select(a => a.ToString()).ToArray();
    private readonly string[] _skills = TraitMetadata.AllSkills.Select(s => s.ToString()).ToArray();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _diceRollSubscription = SessionClient.SubscribeDiceRollReceived(HandleDiceRollReceived);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (IsVisible && !_rollerWasVisible && IsRiteExtendedMode)
        {
            _riteRollsUsed = 0;
            _riteAccumulatedSuccesses = 0;
            _riteFailureNeedsChoice = false;
            _riteRollInFlight = false;
            _riteHadExceptionalSuccess = false;
            _addRiteDisciplineDotsToPotency = false;
        }

        _rollerWasVisible = IsVisible;
        await RefreshResolverTotalAsync();
    }

    private async Task OnAssociatedTraitFromPanelAsync(string name)
    {
        _associatedTraitName = name;
        await RefreshResolverTotalAsync();
    }

    private Task OnModifierFromPanelAsync(int value)
    {
        _modifier = value;
        return Task.CompletedTask;
    }

    private Task OnTenAgainFromPanelAsync(bool value)
    {
        _tenAgain = value;
        return Task.CompletedTask;
    }

    private Task OnNineAgainFromPanelAsync(bool value)
    {
        _nineAgain = value;
        return Task.CompletedTask;
    }

    private Task OnEightAgainFromPanelAsync(bool value)
    {
        _eightAgain = value;
        return Task.CompletedTask;
    }

    private Task OnIsRoteFromPanelAsync(bool value)
    {
        _isRote = value;
        return Task.CompletedTask;
    }

    private async Task RefreshResolverTotalAsync()
    {
        _resolverTotal = null;
        if (Character == null || !IsVisible || FixedDicePool.HasValue)
        {
            return;
        }

        PoolDefinition? pool = SheetPoolBuilder.TryCreate(TraitName, _associatedTraitName);
        if (pool != null)
        {
            _resolverTotal = await TraitResolver.ResolvePoolAsync(Character, pool);
        }
    }

    private int AssociatedTraitDice
    {
        get
        {
            if (string.IsNullOrEmpty(_associatedTraitName) || Character == null)
            {
                return 0;
            }

            var propName = _associatedTraitName.Replace(" ", string.Empty);
            if (TraitMetadata.IsAttribute(propName))
            {
                return Character.GetAttributeRating(propName);
            }

            return Character.GetSkillRating(propName);
        }
    }

    private int? _resolverTotal;

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

    private int TotalPool
    {
        get
        {
            if (IsRiteExtendedMode)
            {
                return Math.Max(0, FixedDicePool ?? 0);
            }

            int basePart = FixedDicePool ?? _resolverTotal ?? (BaseDice + AssociatedTraitDice);
            return Math.Max(0, basePart + _modifier);
        }
    }

    private bool IsChanceDiePool => TotalPool <= 0;

    private static bool IsDieShownAsSuccess(int dieFace, bool isChanceDie) =>
        isChanceDie ? dieFace == 10 : dieFace >= 8;

    private async Task Close()
    {
        IsVisible = false;
        _lastResultDto = null;
        _shareSlug = null;
        _associatedTraitName = string.Empty;
        _modifier = 0;
        _resolverTotal = null;
        _riteRollsUsed = 0;
        _riteAccumulatedSuccesses = 0;
        _riteFailureNeedsChoice = false;
        _riteRollInFlight = false;
        _riteHadExceptionalSuccess = false;
        _addRiteDisciplineDotsToPotency = false;
        await IsVisibleChanged.InvokeAsync(IsVisible);
    }

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

    /// <inheritdoc />
    public void Dispose()
    {
        _diceRollSubscription?.Dispose();
    }
}
