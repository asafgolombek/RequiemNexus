using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

#pragma warning disable SA1201 // Lifecycle and cookie helpers — partial class split across files
#pragma warning disable SA1202

/// <summary>Initiative tracker page — partial class split: State, EncounterLoad, Session, lifecycle (this file), AddParticipant, Announcements, Tilts, NpcCombat, EncounterFlow, Modals, Display.</summary>
public partial class InitiativeTracker : IAsyncDisposable
{
    protected override async Task OnInitializedAsync()
    {
        _persistingSubscription = ApplicationState.RegisterOnPersisting(PersistCookieHeader);

        if (!ApplicationState.TryTakeFromJson<string>("rnCookieHeader", out _cookieHeader))
        {
            _cookieHeader = HttpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        }

        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(_currentUserId))
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        RegisterSessionSignalHandlers();

        await LoadEncounter(showFullPageSpinner: true);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        if (EncounterId == _lastLoadedEncounterId && CampaignId == _lastLoadedCampaignId)
        {
            return;
        }

        await LoadEncounter(showFullPageSpinner: true);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        if (!firstRender && _hubConnectedCampaignId == CampaignId)
        {
            return;
        }

        _hubConnectedCampaignId = CampaignId;
        RefreshCookieFromHttpContext();
        _ = await SessionClient.GetSessionActiveAsync(CampaignId, SessionService);
        _ = await SessionClient.StartAsync(CampaignId, null, _currentUserId, _cookieHeader);
    }

    private void RefreshCookieFromHttpContext()
    {
        string? fromCtx = HttpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        if (!string.IsNullOrWhiteSpace(fromCtx))
        {
            _cookieHeader = fromCtx;
        }
    }

    private Task PersistCookieHeader()
    {
        ApplicationState.PersistAsJson("rnCookieHeader", _cookieHeader);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        _persistingSubscription.Dispose();
        UnregisterSessionSignalHandlers();

        // Same as CharacterDetails: avoid StopAsync on dispose so returning to the campaign cannot
        // schedule a delayed hub teardown after the campaign page has reconnected.
        _disposeCts.Cancel();
        _loadEncounterCts?.Cancel();
        _loadEncounterCts?.Dispose();
        _disposeCts.Dispose();
        return ValueTask.CompletedTask;
    }
}
