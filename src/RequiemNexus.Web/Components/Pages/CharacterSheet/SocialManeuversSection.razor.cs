using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.CharacterSheet;

#pragma warning disable SA1133 // [Parameter][EditorRequired] on one line matches sibling components
#pragma warning disable SA1201 // Field / property ordering — inject + parameter pattern for Blazor code-behind
#pragma warning disable SA1202 // Public partial API ordering vs. private inject fields
#pragma warning disable SA1214 // Readonly vs non-readonly field interleave for maneuver UI state

/// <summary>
/// Player-facing social maneuver list on the character sheet. Markup uses <c>SocialManeuversSectionParts/</c>.
/// </summary>
public partial class SocialManeuversSection : IDisposable
{
    [Inject]
    private ISocialManeuveringService SocialManeuveringService { get; set; } = default!;

    [Inject]
    private ISocialManeuverQueryService SocialManeuverQueryService { get; set; } = default!;

    [Inject]
    private ISocialManeuverRollService SocialManeuverRollService { get; set; } = default!;

    [Inject]
    private SessionClientService SessionClient { get; set; } = default!;

    [Inject]
    private ToastService ToastService { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public int CharacterId { get; set; }

    [Parameter]
    [EditorRequired]
    public string CurrentUserId { get; set; } = string.Empty;

    private List<SocialManeuver> _maneuvers = [];

    private bool _loading = true;

    private bool _busy;

    private readonly Dictionary<int, int> _openDoorPoolByManeuverId = [];

    private readonly Dictionary<int, int> _forceDoorPoolByManeuverId = [];

    private readonly Dictionary<int, bool> _forceHardLeverageByManeuverId = [];

    private readonly Dictionary<int, int> _forceBpSeverityByManeuverId = [];

    private readonly Dictionary<int, string> _spendBenefitByClueId = [];

    private readonly SemaphoreSlim _loadManeuversLock = new(1, 1);

    private System.Timers.Timer? _countdownTimer;

    private IDisposable? _socialManeuverSubscription;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            _loading = false;
            return;
        }

        await LoadManeuversAsync();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && _socialManeuverSubscription == null)
        {
            _socialManeuverSubscription = SessionClient.SubscribeSocialManeuverUpdated(OnSocialManeuverUpdated);
            _countdownTimer = new System.Timers.Timer(1000);
            _countdownTimer.Elapsed += (_, _) => _ = InvokeAsync(() =>
            {
                StateHasChanged();
            });
            _countdownTimer.AutoReset = true;
            _countdownTimer.Start();
        }
    }

    private void OnSocialManeuverUpdated(SocialManeuverUpdateDto dto)
    {
        if (dto.InitiatorCharacterId != CharacterId)
        {
            return;
        }

        _ = InvokeAsync(async () =>
        {
            await LoadManeuversAsync();
            StateHasChanged();
        });
    }

    private async Task LoadManeuversAsync()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return;
        }

        await _loadManeuversLock.WaitAsync();
        try
        {
            _loading = true;
            try
            {
                _maneuvers = (await SocialManeuverQueryService.ListForInitiatorAsync(CharacterId, CurrentUserId)).ToList();
                foreach (SocialManeuver m in _maneuvers)
                {
                    _openDoorPoolByManeuverId.TryAdd(m.Id, 5);
                    _forceDoorPoolByManeuverId.TryAdd(m.Id, 5);
                    _forceHardLeverageByManeuverId.TryAdd(m.Id, false);
                    _forceBpSeverityByManeuverId.TryAdd(m.Id, 7);
                    foreach (ManeuverClue clue in m.Clues)
                    {
                        _spendBenefitByClueId.TryAdd(clue.Id, string.Empty);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                _maneuvers = [];
            }
            finally
            {
                _loading = false;
            }
        }
        finally
        {
            _loadManeuversLock.Release();
        }
    }

    private Task HandlePoolsOrBenefitsChangedAsync() => InvokeAsync(StateHasChanged);

    private int GetOpenPool(int id) => _openDoorPoolByManeuverId.GetValueOrDefault(id, 5);

    private int GetForcePool(int id) => _forceDoorPoolByManeuverId.GetValueOrDefault(id, 5);

    private bool GetForceHard(int id) => _forceHardLeverageByManeuverId.GetValueOrDefault(id);

    private int GetForceBp(int id) => _forceBpSeverityByManeuverId.GetValueOrDefault(id, 7);

    private async Task SpendClueAsync(int clueId)
    {
        string benefit = _spendBenefitByClueId.GetValueOrDefault(clueId, string.Empty).Trim();
        if (string.IsNullOrEmpty(benefit))
        {
            ToastService.Show("Clue", "Enter the recorded benefit.", ToastType.Warning);
            return;
        }

        _busy = true;
        try
        {
            await SocialManeuveringService.SpendManeuverClueAsync(clueId, benefit, CurrentUserId);
            _spendBenefitByClueId[clueId] = string.Empty;
            ToastService.Show("Clue", "Clue spent.", ToastType.Success);
            await LoadManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Clue", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RollOpenAsync(int maneuverId)
    {
        _busy = true;
        try
        {
            int pool = GetOpenPool(maneuverId);
            Result<(SocialManeuver Updated, RollResult Roll, int DoorsOpened)> openResult =
                await SocialManeuverRollService.RollOpenDoorAsync(maneuverId, pool, CurrentUserId);
            if (!openResult.IsSuccess)
            {
                ToastService.Show("Open Door", openResult.Error ?? "Roll failed.", ToastType.Error);
                return;
            }

            (_, RollResult roll, int opened) = openResult.Value!;
            ToastService.Show(
                "Open Door",
                $"{opened} door(s). Successes: {roll.Successes}",
                ToastType.Info);
            await LoadManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Open Door", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RollForceAsync(int maneuverId)
    {
        bool ok = await JSRuntime.InvokeAsync<bool>(
            "rnConfirm",
            "Force Doors: on failure you can never use Social maneuvering against this NPC again (Burnt). Continue?");
        if (!ok)
        {
            return;
        }

        _busy = true;
        try
        {
            int pool = GetForcePool(maneuverId);
            Result<(SocialManeuver Updated, RollResult Roll, bool ForcedSuccess)> forceResult =
                await SocialManeuverRollService.RollForceDoorsAsync(
                    maneuverId,
                    pool,
                    GetForceHard(maneuverId),
                    GetForceBp(maneuverId),
                    CurrentUserId);
            if (!forceResult.IsSuccess)
            {
                ToastService.Show("Force Doors", forceResult.Error ?? "Roll failed.", ToastType.Error);
                return;
            }

            (_, RollResult roll, bool forcedOk) = forceResult.Value!;
            ToastService.Show(
                "Force Doors",
                forcedOk ? $"Success. Successes: {roll.Successes}" : $"Failed — Burnt. Successes: {roll.Successes}",
                forcedOk ? ToastType.Success : ToastType.Warning);
            await LoadManeuversAsync();
        }
        catch (Exception ex)
        {
            ToastService.Show("Force Doors", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _socialManeuverSubscription?.Dispose();

        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _loadManeuversLock.Dispose();
    }
}
