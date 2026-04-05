using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.Models;
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
/// - Overview tab composes <c>StorytellerGlimpseOverviewParts</c> (pinned NPCs, character vitals grid, aura, coterie Beat, degeneration banners).
/// - Feature partials: <c>StorytellerGlimpse.Awards</c>, <c>StorytellerGlimpse.Chrome</c>, <c>StorytellerGlimpse.SignalR</c>, <c>StorytellerGlimpse.PassiveAura</c>, <c>StorytellerGlimpse.Degeneration</c>.
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

    private IDisposable? _chronicleSubscription;

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

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _chronicleSubscription?.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task LoadSocialManeuvers()
    {
        _socialManeuvers = (await SocialManeuveringService.ListForCampaignAsync(Id, _currentUserId!)).ToList();
        _npcs = await ChronicleNpcService.GetNpcsAsync(Id);
    }
}
