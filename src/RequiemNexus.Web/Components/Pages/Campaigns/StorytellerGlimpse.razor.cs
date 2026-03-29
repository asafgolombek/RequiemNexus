using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.JSInterop;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Models;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Helpers;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

#pragma warning disable SA1201
#pragma warning disable SA1202
#pragma warning disable SA1204
#pragma warning disable SA1214

/// <summary>
/// Storyteller dashboard for campaign management.
/// Decomposition:
/// - <see cref="GlimpseSocialManeuvers"/> handles Phase 10 social state.
/// - <see cref="GlimpsePendingRequests"/> handles approvals (Bloodlines, Covenants, etc).
/// - Main page manages coterie vitals and awards.
/// </summary>
public partial class StorytellerGlimpse : IAsyncDisposable
{
    [Inject]
    private SessionClientService SessionClient { get; set; } = default!;

    [Inject]
    private ISessionService SessionService { get; set; } = default!;

    [Inject]
    private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    [Inject]
    private IHumanityService HumanityService { get; set; } = default!;

    [Inject]
    private ITouchstoneService TouchstoneService { get; set; } = default!;

    [Inject]
    private IPredatoryAuraService PredatoryAuraService { get; set; } = default!;

    /// <summary>Gets or sets the campaign id.</summary>
    [Parameter]
    public int Id { get; set; }

    private List<CharacterVitalsDto> _vitals = [];

    private Campaign _campaign = default!;

    private List<SocialManeuver> _socialManeuvers = [];

    private List<ChronicleNpc> _npcs = [];

    private bool _loading = true;

    private bool _accessDenied;

    private bool _awarding;

    private string? _currentUserId;

    // Per-character award state
    private Dictionary<int, string> _beatReasons = [];

    private Dictionary<int, string> _xpReasons = [];

    private Dictionary<int, int> _xpAmounts = [];

    private Dictionary<int, string> _feedbackMessages = [];

    // Coterie award
    private string _coteReason = string.Empty;

    private string _coteMessage = string.Empty;

    // Pending Lists
    private List<BloodlineApplicationDto> _pendingBloodlines = [];

    private List<CovenantApplicationDto> _pendingCovenants = [];

    private List<RiteApplicationDto> _pendingRites = [];

    private List<ChosenMysteryApplicationDto> _pendingChosenMysteries = [];

    private List<CoilApplicationDto> _pendingCoils = [];

    private List<PendingAssetProcurementDto> _pendingAssetProcurements = [];

    private readonly HashSet<int> _pinnedNpcIds = [];

    private int? _activeDropTargetId;

    private bool _lineageModalOpen;

    private int _lineageEditCharacterId;

    private string _glimpseTab = "overview";

    private readonly Dictionary<int, (string Name, int Humanity, int Resolve)> _degAlerts = [];

    private bool _degRollModalOpen;

    private int? _degRollTargetId;

    private int _passiveAuraCharacterA;

    private int _passiveAuraCharacterB;

    private bool _passiveAuraBusy;

    private string _passiveAuraMessage = string.Empty;

    private bool _passiveAuraError;

    private bool _sessionHubWired;

    private int PendingApprovalCount =>
        _pendingBloodlines.Count
        + _pendingCovenants.Count
        + _pendingRites.Count
        + _pendingChosenMysteries.Count
        + _pendingCoils.Count
        + _pendingAssetProcurements.Count;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(_currentUserId))
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        await LoadVitals();
    }

    private async Task LoadVitals()
    {
        _loading = true;
        try
        {
            _campaign = await CampaignService.GetCampaignByIdAsync(Id, _currentUserId!)
                ?? throw new UnauthorizedAccessException();

            _vitals = await GlimpseService.GetCampaignVitalsAsync(Id, _currentUserId!);
            _pendingBloodlines = await BloodlineService.GetPendingBloodlineApplicationsAsync(Id, _currentUserId!);
            _pendingCovenants = await CovenantService.GetPendingCovenantApplicationsAsync(Id, _currentUserId!);
            _pendingRites = await SorceryService.GetPendingRiteApplicationsAsync(Id, _currentUserId!);
            _pendingChosenMysteries = await CoilService.GetPendingChosenMysteryApplicationsAsync(Id, _currentUserId!);
            _pendingCoils = await CoilService.GetPendingCoilApplicationsAsync(Id, _currentUserId!);
            _pendingAssetProcurements = (await AssetProcurementService.GetPendingForCampaignAsync(Id, _currentUserId!)).ToList();

            await LoadSocialManeuvers();

            foreach (var v in _vitals)
            {
                _beatReasons.TryAdd(v.CharacterId, string.Empty);
                _xpReasons.TryAdd(v.CharacterId, string.Empty);
                _xpAmounts.TryAdd(v.CharacterId, 1);
                _feedbackMessages.TryAdd(v.CharacterId, string.Empty);
            }

            _accessDenied = false;
            SyncDegenerationAlertsFromVitals();
        }
        catch (UnauthorizedAccessException)
        {
            _accessDenied = true;
        }
        finally
        {
            _loading = false;
        }
    }

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

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_sessionHubWired)
        {
            SessionClient.ChronicleUpdated -= HandleChroniclePatch;
        }

        return ValueTask.CompletedTask;
    }

    private async Task LoadSocialManeuvers()
    {
        _socialManeuvers = (await SocialManeuveringService.ListForCampaignAsync(Id, _currentUserId!)).ToList();
        _npcs = await ChronicleNpcService.GetNpcsAsync(Id);
    }

    private void HandleDragStart(DragEventArgs e)
    {
        e.DataTransfer.EffectAllowed = "move";
    }

    private void HandleDragEnter(int id)
    {
        _activeDropTargetId = id;
    }

    private void HandleDragLeave()
    {
        _activeDropTargetId = null;
    }

    private async Task HandleDrop(int characterId)
    {
        _activeDropTargetId = null;
        if (_awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardBeatToCharacterAsync(Id, characterId, "Drag to award Beat", _currentUserId!);
            ToastService.Show("Success", "Beat awarded via drag-drop.", ToastType.Success);
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task AwardBeat(int characterId)
    {
        if (string.IsNullOrWhiteSpace(_beatReasons[characterId]) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardBeatToCharacterAsync(Id, characterId, _beatReasons[characterId], _currentUserId!);
            _beatReasons[characterId] = string.Empty;
            _feedbackMessages[characterId] = "Beat awarded.";
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task AwardXp(int characterId)
    {
        if (_xpAmounts[characterId] <= 0 || string.IsNullOrWhiteSpace(_xpReasons[characterId]) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardXpToCharacterAsync(Id, characterId, _xpAmounts[characterId], _xpReasons[characterId], _currentUserId!);
            _xpReasons[characterId] = string.Empty;
            _feedbackMessages[characterId] = "XP awarded.";
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private async Task AwardCoterieBeat()
    {
        if (string.IsNullOrWhiteSpace(_coteReason) || _awarding)
        {
            return;
        }

        _awarding = true;
        try
        {
            await GlimpseService.AwardBeatToCampaignAsync(Id, _coteReason, _currentUserId!);
            _coteReason = string.Empty;
            _coteMessage = "Beat awarded to all.";
            await LoadVitals();
        }
        finally
        {
            _awarding = false;
        }
    }

    private void TogglePinNpc(int id)
    {
        if (_pinnedNpcIds.Contains(id))
        {
            _pinnedNpcIds.Remove(id);
        }
        else
        {
            _pinnedNpcIds.Add(id);
        }
    }

    private static int BarPct(int cur, int max)
    {
        return max <= 0 ? 0 : Math.Clamp(cur * 100 / max, 0, 100);
    }

    private void OpenLineageEditor(int characterId)
    {
        _lineageEditCharacterId = characterId;
        _lineageModalOpen = true;
    }

    private async Task OnLineageSavedAsync()
    {
        await LoadVitals();
    }

    private void SelectGlimpseTab(string tab) => _glimpseTab = tab;

    private void HandleGlimpseTabKeydown(KeyboardEventArgs e)
    {
        string[] tabs = ["overview", "social", "approvals", "chronicle"];
        int i = Array.IndexOf(tabs, _glimpseTab);
        if (i < 0)
        {
            return;
        }

        if (e.Key == "ArrowRight" || e.Key == "ArrowDown")
        {
            SelectGlimpseTab(tabs[(i + 1) % tabs.Length]);
        }
        else if (e.Key == "ArrowLeft" || e.Key == "ArrowUp")
        {
            SelectGlimpseTab(tabs[(i - 1 + tabs.Length) % tabs.Length]);
        }
        else if (e.Key == "Home")
        {
            SelectGlimpseTab(tabs[0]);
        }
        else if (e.Key == "End")
        {
            SelectGlimpseTab(tabs[^1]);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _accessDenied || string.IsNullOrEmpty(_currentUserId) || _sessionHubWired)
        {
            return;
        }

        _sessionHubWired = true;
        SessionClient.ChronicleUpdated += HandleChroniclePatch;
        string? cookie = HttpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        _ = await SessionClient.GetSessionActiveAsync(Id, SessionService);
        SessionHubConnectResult hubResult = await SessionClient.StartAsync(Id, null, _currentUserId, cookie);
        if (hubResult != SessionHubConnectResult.Connected)
        {
            ToastService.Show(
                "Live session",
                SessionHubConnectMessages.Format(hubResult),
                ToastType.Warning);
        }
    }

    private void HandleChroniclePatch(ChronicleUpdateDto patch)
    {
        if (patch.ChronicleId != Id)
        {
            return;
        }

        if (patch.DegenerationCheckRequired is { } alert)
        {
            _degAlerts[alert.CharacterId] = (alert.CharacterName, alert.Humanity, alert.ResolveRating);
        }

        if (patch.DegenerationCheckClearedCharacterId is int cleared)
        {
            _degAlerts.Remove(cleared);
        }

        _ = InvokeAsync(StateHasChanged);
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

    private void HandleDegBannerKeydown(KeyboardEventArgs e, int characterId)
    {
        if (e.Key == "Enter" || e.Key == " ")
        {
            OpenDegenerationRollModal(characterId);
        }
    }

    private static string DegenerationPoolHint(int humanity, int resolve)
    {
        if (humanity <= 0)
        {
            return "chance die (Humanity 0)";
        }

        int pool = resolve + (7 - humanity);
        return $"{resolve} + (7 − {humanity}) = {pool} dice";
    }

    private string? DegenerationModalMessage =>
        _degRollTargetId is int id && _degAlerts.TryGetValue(id, out (string Name, int Humanity, int Resolve) entry)
            ? $"{entry.Name} rolls degeneration ({DegenerationPoolHint(entry.Humanity, entry.Resolve)}). " +
              "Success (≥1): clear all stains, Humanity unchanged. " +
              "Failure: lose 1 Humanity, clear stains. Dramatic failure: also gain Guilty."
            : string.Empty;
}
