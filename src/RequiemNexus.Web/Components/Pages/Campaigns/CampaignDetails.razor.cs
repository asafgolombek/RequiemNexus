using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Components.UI;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Helpers;
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

    private readonly AddCharacterModel _addModel = new();
    private int? _perceptionOpenId;
    private bool _perceptionBusy;
    private readonly Dictionary<int, string> _perceptionResults = new();

    /// <summary>Bound from ST perception panel (Wits + Awareness vs Composure).</summary>
    private bool PerceptionUseAwareness { get; set; }

    /// <summary>Penalty dice for hidden perception roll.</summary>
    private int PerceptionPenalty { get; set; }

    private string _rosterTab = "players";

    private bool _inviteModalOpen;

    private string RosterTabClass(string tab) => _rosterTab == tab ? "active" : string.Empty;

    private void SelectRosterTab(string tab) => _rosterTab = tab;

    private void HandleRosterTabKeydown(KeyboardEventArgs e)
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
    }

    private Task CloseInviteModal()
    {
        _inviteModalOpen = false;
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

    private Task PersistCookieHeader()
    {
        ApplicationState.PersistAsJson("rnCookieHeader", _cookieHeader);
        return Task.CompletedTask;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            RefreshCookieFromHttpContext();

            SessionHubConnectResult hubResult;
            if (_sessionActive && _campaign != null && !CampaignService.IsStoryteller(_campaign, _currentUserId))
            {
                hubResult = await JoinSessionCoreAsync();
            }
            else
            {
                // We don't specify characterId here as we might have multiple or none
                hubResult = await SessionClient.StartAsync(Id, null, _currentUserId, _cookieHeader);
            }

            _sessionHubMessage = hubResult == SessionHubConnectResult.Connected ? null : SessionHubConnectMessages.Format(hubResult);

            // Hydrate initial presence from REST API so we show existing players before hub broadcasts
            var state = await SessionService.GetSessionStateAsync(Id);
            if (state?.Presence != null)
            {
                SessionClient.SetPresence(state.Presence);
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (HttpRequestException)
        {
            // Optional: surface an error without crashing the page.
            // _errorMessage = "Unable to connect to the realtime session.";
        }
    }

    private void HandleSessionStarted()
    {
        _sessionActive = true;
        StartHeartbeatTimerIfStoryteller();
        InvokeAsync(StateHasChanged);
    }

    private void HandleSessionEnded(string reason)
    {
        _sessionActive = false;
        StopHeartbeatTimer();
        InvokeAsync(StateHasChanged);
    }

    private void StartHeartbeatTimerIfStoryteller()
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId) || !CampaignService.IsStoryteller(_campaign, _currentUserId))
        {
            return;
        }

        StopHeartbeatTimer();
        _heartbeatTimer = new Timer(
            _ =>
            {
                if (_sessionActive && _campaign != null)
                {
                    _ = SessionClient.SendHeartbeatAsync();
                }
            },
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
    }

    private void StopHeartbeatTimer()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    private async Task StartSession()
    {
        await SessionClient.StartSessionAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task EndSession()
    {
        await SessionClient.EndSessionAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task JoinSession()
    {
        RefreshCookieFromHttpContext();
        SessionHubConnectResult hubResult = await JoinSessionCoreAsync();
        _sessionHubMessage = hubResult == SessionHubConnectResult.Connected ? null : SessionHubConnectMessages.Format(hubResult);
        await InvokeAsync(StateHasChanged);
    }

    private async Task<SessionHubConnectResult> JoinSessionCoreAsync()
    {
        int? characterId = _campaign?.Characters.FirstOrDefault(c => c.ApplicationUserId == _currentUserId)?.Id;
        return await SessionClient.StartAsync(Id, characterId, _currentUserId!, _cookieHeader);
    }

    private void RefreshCookieFromHttpContext()
    {
        string? fromCtx = HttpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrWhiteSpace(fromCtx))
        {
            _cookieHeader = fromCtx;
        }
    }

    private async Task LeaveSession()
    {
        await SessionClient.StopAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void NavigateToCharacter(int characterId, bool isOwner)
    {
        if (isOwner)
        {
            NavigationManager.NavigateTo($"/character/{characterId}");
        }
        else
        {
            NavigationManager.NavigateTo($"/campaigns/{Id}/characters/{characterId}");
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

    private async Task CreateLore()
    {
        if (string.IsNullOrWhiteSpace(_newLoreTitle) || _campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        await CampaignService.CreateLoreAsync(_campaign.Id, _newLoreTitle.Trim(), _newLoreBody.Trim(), _currentUserId);
        _newLoreTitle = string.Empty;
        _newLoreBody = string.Empty;
        _showAddLoreForm = false;
        _loreEntries = await CampaignService.GetLoreAsync(_campaign.Id);
    }

    private void DiscardAddLore()
    {
        _newLoreTitle = string.Empty;
        _newLoreBody = string.Empty;
        _showAddLoreForm = false;
    }

    private async Task DeleteLore(int loreId)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        await CampaignService.DeleteLoreAsync(loreId, _currentUserId);
        _loreEntries = await CampaignService.GetLoreAsync(_campaign!.Id);
    }

    private async Task CreateSessionPrepNote()
    {
        if (string.IsNullOrWhiteSpace(_newPrepTitle) || _campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        await CampaignService.CreateSessionPrepNoteAsync(_campaign.Id, _newPrepTitle.Trim(), _newPrepBody.Trim(), _currentUserId);
        _newPrepTitle = string.Empty;
        _newPrepBody = string.Empty;
        _showAddPrepForm = false;
        _sessionPrepNotes = await CampaignService.GetSessionPrepNotesAsync(_campaign.Id, _currentUserId);
    }

    private void DiscardAddPrep()
    {
        _newPrepTitle = string.Empty;
        _newPrepBody = string.Empty;
        _showAddPrepForm = false;
    }

    private async Task DeleteSessionPrepNote(int noteId)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        await CampaignService.DeleteSessionPrepNoteAsync(noteId, _currentUserId);
        _sessionPrepNotes = await CampaignService.GetSessionPrepNotesAsync(_campaign!.Id, _currentUserId);
    }

    private void ToggleAddCharacter()
    {
        _showAddCharacter = !_showAddCharacter;
        _addModel.CharacterId = 0;
    }

    private void CancelConfirm()
    {
        _showConfirmDelete = false;
        _showConfirmLeave = false;
        _confirmRemoveCharacterId = 0;
    }

    private void AskRemoveCharacter(int characterId)
    {
        _confirmRemoveCharacterId = characterId;
    }

    private async Task ConfirmRemoveCharacter(int characterId)
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await CampaignService.RemoveCharacterFromCampaignAsync(_campaign.Id, characterId, _currentUserId);
            _confirmRemoveCharacterId = 0;
            await LoadData();
        }
        catch (Exception ex)
        {
            ToastService.Show("Campaign", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ConfirmLeaveCampaign()
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await CampaignService.LeaveCampaignAsync(_campaign.Id, _currentUserId);
            _showConfirmLeave = false;
            await LoadData();
        }
        catch (Exception ex)
        {
            ToastService.Show("Campaign", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ConfirmDeleteCampaign()
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await CampaignService.DeleteCampaignAsync(_campaign.Id, _currentUserId);
            NavigationManager.NavigateTo("/campaigns");
        }
        catch (Exception ex)
        {
            ToastService.Show("Campaign", ex.Message, ToastType.Error);
            _busy = false;
        }
    }

    private async Task AddCharacterSubmit()
    {
        if (_addModel.CharacterId > 0 && _campaign != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CampaignService.AddCharacterToCampaignAsync(_campaign.Id, _addModel.CharacterId, _currentUserId);
            await LoadData();
            _showAddCharacter = false;
        }
    }

    private async Task RegenerateJoinInvite()
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _inviteBusy = true;
        try
        {
            string token = await CampaignService.RegenerateJoinInviteAsync(_campaign.Id, _currentUserId);
            string path = $"/campaigns/{_campaign.Id}/join?invite={Uri.EscapeDataString(token)}";
            _lastGeneratedJoinUrl = NavigationManager.ToAbsoluteUri(path).AbsoluteUri;
            await LoadData();
        }
        catch (Exception ex)
        {
            ToastService.Show("Invite link", ex.Message, ToastType.Error);
        }
        finally
        {
            _inviteBusy = false;
        }
    }

    private async Task ClearJoinInvite()
    {
        if (_campaign == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _inviteBusy = true;
        try
        {
            await CampaignService.ClearJoinInviteAsync(_campaign.Id, _currentUserId);
            _lastGeneratedJoinUrl = null;
            await LoadData();
        }
        catch (Exception ex)
        {
            ToastService.Show("Invite link", ex.Message, ToastType.Error);
        }
        finally
        {
            _inviteBusy = false;
        }
    }

    private async Task CopyJoinUrl()
    {
        if (string.IsNullOrEmpty(_lastGeneratedJoinUrl))
        {
            return;
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _lastGeneratedJoinUrl);
        }
        catch (JSException)
        {
            // Clipboard may be unavailable; URL remains in the field for manual copy.
        }
    }

    private void TogglePerception(int characterId)
    {
        _perceptionOpenId = _perceptionOpenId == characterId ? null : characterId;
    }

    private async Task RollPerception(int characterId)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _perceptionBusy = true;
        StateHasChanged();
        try
        {
            PerceptionRollResultDto result = await PerceptionRollService.RollPerceptionAsync(
                characterId,
                PerceptionUseAwareness,
                PerceptionPenalty,
                _currentUserId);
            string dice = string.Join(", ", result.DiceRolled);
            _perceptionResults[characterId] =
                $"{result.PoolDescription}: [{dice}] → {result.Successes} successes" +
                (result.IsExceptionalSuccess ? " (exceptional)" : string.Empty) +
                (result.IsDramaticFailure ? " (dramatic failure)" : string.Empty);
        }
        catch (Exception ex)
        {
            _perceptionResults[characterId] = ex.Message;
        }
        finally
        {
            _perceptionBusy = false;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        StopHeartbeatTimer();
        _persistingSubscription.Dispose();
        _sessionStartedSubscription?.Dispose();
        _sessionEndedSubscription?.Dispose();
    }

    public class AddCharacterModel
    {
        public int CharacterId { get; set; }
    }
}
