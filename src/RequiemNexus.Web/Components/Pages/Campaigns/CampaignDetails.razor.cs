using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Components.Pages.Campaigns.CampaignDetailsParts;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

#pragma warning disable SA1201
#pragma warning disable SA1202 // Member order matches original @code block
#pragma warning disable SA1203
#pragma warning disable SA1214

public partial class CampaignDetails : IDisposable
{
    [Parameter]
    public int Id { get; set; }

    private Campaign? _campaign;
    private List<Character> _availableCharacters = [];
    private bool _showAddCharacter = false;
    private string? _currentUserId;
    private bool _showConfirmDelete = false;
    private bool _showConfirmLeave = false;
    private int _confirmRemoveCharacterId = 0;
    private bool _busy = false;
    private List<CampaignLore> _loreEntries = [];
    private List<SessionPrepNote> _sessionPrepNotes = [];
    private string _newLoreTitle = string.Empty;
    private string _newLoreBody = string.Empty;
    private bool _showAddLoreForm;

    private bool _showAddPrepForm;
    private string _newPrepTitle = string.Empty;
    private string _newPrepBody = string.Empty;
    private bool _sessionActive = false;
    private bool _campaignLoadComplete;
    private string? _sessionHubMessage;
    private bool _inviteBusy;
    private string? _lastGeneratedJoinUrl;
    private string? _cookieHeader;
    private PersistingComponentStateSubscription _persistingSubscription;
    private Timer? _heartbeatTimer;

    private IDisposable? _sessionStartedSubscription;
    private IDisposable? _sessionEndedSubscription;

    private readonly AddCharacterToCampaignModel _addModel = new();
    private int? _perceptionOpenId;
    private bool _perceptionBusy;
    private readonly Dictionary<int, string> _perceptionResults = new();

    /// <summary>Bound from ST perception panel (Wits + Awareness vs Composure).</summary>
    private bool PerceptionUseAwareness { get; set; }

    /// <summary>Penalty dice for hidden perception roll.</summary>
    private int PerceptionPenalty { get; set; }

    private string _rosterTab = "players";

    private bool _inviteModalOpen;

    private async Task OnRosterTabChangedAsync(string tab)
    {
        _rosterTab = tab;
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleRosterTabKeydownAsync(KeyboardEventArgs e)
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        bool isSt = CampaignService.IsStoryteller(_campaign, _currentUserId);
        string[] tabs = isSt ? ["players", "lore", "prep"] : ["players", "lore"];
        int i = Array.IndexOf(tabs, _rosterTab);
        if (i < 0)
        {
            return;
        }

        if (e.Key == "ArrowRight" || e.Key == "ArrowDown")
        {
            _rosterTab = tabs[(i + 1) % tabs.Length];
        }
        else if (e.Key == "ArrowLeft" || e.Key == "ArrowUp")
        {
            _rosterTab = tabs[(i - 1 + tabs.Length) % tabs.Length];
        }
        else if (e.Key == "Home")
        {
            _rosterTab = tabs[0];
        }
        else if (e.Key == "End")
        {
            _rosterTab = tabs[^1];
        }
        else
        {
            return;
        }

        await InvokeAsync(StateHasChanged);
    }

    private Task OnPerceptionUseAwarenessChanged(bool value)
    {
        PerceptionUseAwareness = value;
        return Task.CompletedTask;
    }

    private Task OnPerceptionPenaltyChanged(int value)
    {
        PerceptionPenalty = value;
        return Task.CompletedTask;
    }

    private Task OnNavigateToCharacterAsync((int CharacterId, bool IsOwner) arg)
    {
        NavigateToCharacter(arg.CharacterId, arg.IsOwner);
        return Task.CompletedTask;
    }

    private Task RequestLeaveCampaignAsync()
    {
        _showConfirmLeave = true;
        return Task.CompletedTask;
    }

    private Task RequestDeleteCampaignAsync()
    {
        _showConfirmDelete = true;
        return Task.CompletedTask;
    }

    private Task CancelConfirmAsync()
    {
        CancelConfirm();
        return Task.CompletedTask;
    }

    private Task OpenLoreAddFormAsync()
    {
        _showAddLoreForm = true;
        return Task.CompletedTask;
    }

    private Task SetNewLoreTitleAsync(string value)
    {
        _newLoreTitle = value;
        return Task.CompletedTask;
    }

    private Task SetNewLoreBodyAsync(string value)
    {
        _newLoreBody = value;
        return Task.CompletedTask;
    }

    private Task OpenPrepAddFormAsync()
    {
        _showAddPrepForm = true;
        return Task.CompletedTask;
    }

    private Task SetNewPrepTitleAsync(string value)
    {
        _newPrepTitle = value;
        return Task.CompletedTask;
    }

    private Task SetNewPrepBodyAsync(string value)
    {
        _newPrepBody = value;
        return Task.CompletedTask;
    }

    protected override async Task OnInitializedAsync()
    {
        _persistingSubscription = ApplicationState.RegisterOnPersisting(PersistCookieHeader);

        if (!ApplicationState.TryTakeFromJson<string>("rnCookieHeader", out _cookieHeader))
        {
            _cookieHeader = HttpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        }

        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(_currentUserId))
        {
            _sessionStartedSubscription = SessionClient.SubscribeSessionStarted(HandleSessionStarted);
            _sessionEndedSubscription = SessionClient.SubscribeSessionEnded(HandleSessionEnded);

            // Check if session is already active (uses cache if available)
            _sessionActive = await SessionClient.GetSessionActiveAsync(Id, SessionService);
        }

        await LoadData();

        if (_sessionActive && _campaign != null && !string.IsNullOrEmpty(_currentUserId) && CampaignService.IsStoryteller(_campaign, _currentUserId))
        {
            StartHeartbeatTimerIfStoryteller();
        }
    }

    private async Task LoadData()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            _campaignLoadComplete = true;
            return;
        }

        _campaign = await CampaignService.GetCampaignByIdAsync(Id, _currentUserId);

        if (_campaign != null)
        {
            // Show only characters that are not yet in any campaign, for the add-character dropdown.
            List<Character> allUserCharacters = await CharacterReader.GetCharactersByUserIdAsync(_currentUserId);
            _availableCharacters = allUserCharacters.Where(c => c.CampaignId == null).ToList();

            _loreEntries = await CampaignService.GetLoreAsync(_campaign.Id);

            if (CampaignService.IsStoryteller(_campaign, _currentUserId))
            {
                _sessionPrepNotes = await CampaignService.GetSessionPrepNotesAsync(_campaign.Id, _currentUserId);
            }
        }

        _campaignLoadComplete = true;
    }

    public void Dispose()
    {
        StopHeartbeatTimer();
        _persistingSubscription.Dispose();
        _sessionStartedSubscription?.Dispose();
        _sessionEndedSubscription?.Dispose();
    }
}
