using System.Net.Http;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Helpers;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

/// <summary>
/// Partial: live session hub, cookie persistence, heartbeat, and join-invite flows for <see cref="CampaignDetails"/>.
/// </summary>
public partial class CampaignDetails
{
    /// <inheritdoc />
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
                hubResult = await SessionClient.StartAsync(Id, null, _currentUserId, _cookieHeader);
            }

            _sessionHubMessage = hubResult == SessionHubConnectResult.Connected ? null : SessionHubConnectMessages.Format(hubResult);

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
        }
    }

    private Task PersistCookieHeader()
    {
        ApplicationState.PersistAsJson("rnCookieHeader", _cookieHeader);
        return Task.CompletedTask;
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

    private Task CloseInviteModal()
    {
        _inviteModalOpen = false;
        return Task.CompletedTask;
    }

    private Task OpenInviteModal()
    {
        _inviteModalOpen = true;
        return Task.CompletedTask;
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
}
