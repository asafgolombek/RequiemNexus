using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
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
public partial class StorytellerGlimpse
{
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

    private readonly HashSet<int> _pinnedNpcIds = [];

    private int? _activeDropTargetId;

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(_currentUserId))
        {
            NavigationManager.NavigateTo("/login");
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

            await LoadSocialManeuvers();

            foreach (var v in _vitals)
            {
                _beatReasons.TryAdd(v.CharacterId, string.Empty);
                _xpReasons.TryAdd(v.CharacterId, string.Empty);
                _xpAmounts.TryAdd(v.CharacterId, 1);
                _feedbackMessages.TryAdd(v.CharacterId, string.Empty);
            }

            _accessDenied = false;
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
}
