// Blazor partial: SignalR session wiring and cookie persistence for CharacterDetails.
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Helpers;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        SessionClient.CharacterUpdated -= HandleCharacterUpdated;
        SessionClient.BloodlineApproved -= HandleBloodlineApproved;
        SessionClient.ChronicleUpdated -= HandleChroniclePatchForCharacter;

        // Do not call SessionClient.StopAsync() here: Blazor may dispose this page after the campaign
        // page has already reconnected; a delayed stop would tear down presence. Hub teardown is via
        // Leave Session on the campaign or circuit dispose when the browser session ends.
        _persistingSubscription.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        if (_character.CampaignId != null)
        {
            RefreshCookieFromHttpContext();
            _ = await SessionClient.GetSessionActiveAsync(_character.CampaignId.Value, SessionService);
            SessionHubConnectResult hubResult = await SessionClient.StartAsync(
                _character.CampaignId.Value,
                _character.Id,
                _currentUserId,
                _cookieHeader);

            if (hubResult != SessionHubConnectResult.Connected)
            {
                ToastService.Show(
                    "Live session",
                    SessionHubConnectMessages.Format(hubResult),
                    ToastType.Warning);
            }

            SessionClient.CharacterUpdated += HandleCharacterUpdated;
            SessionClient.BloodlineApproved += HandleBloodlineApproved;
            SessionClient.ChronicleUpdated += HandleChroniclePatchForCharacter;
        }

        await TryShowRecentBloodlineApprovalToastAsync();

        await InvokeAsync(StateHasChanged);
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

    /// <summary>
    /// Hub events may arrive off the renderer sync context; marshal work onto it and avoid async void.
    /// </summary>
    private void HandleCharacterUpdated(CharacterUpdateDto patch)
    {
        _ = InvokeAsync(async () =>
        {
            if (patch.CharacterId != Id || _character == null || string.IsNullOrEmpty(_currentUserId))
            {
                return;
            }

            if (_skipIncomingCharacterHubReloadCount > 0)
            {
                _skipIncomingCharacterHubReloadCount--;
                await ResolveDisciplinePowerPoolsAsync();
                StateHasChanged();
                return;
            }

            _character = await CharacterService.ReloadCharacterAsync(Id, _currentUserId);
            await ResolveDisciplinePowerPoolsAsync();
            StateHasChanged();
        });
    }

    private void HandleChroniclePatchForCharacter(ChronicleUpdateDto patch)
    {
        if (_character?.CampaignId != patch.ChronicleId)
        {
            return;
        }

        if (patch.DegenerationCheckRequired?.CharacterId == Id || patch.DegenerationCheckClearedCharacterId == Id)
        {
            _ = InvokeAsync(ReloadCharacterFromHubNotificationAsync);
        }
    }

    private async Task ReloadCharacterFromHubNotificationAsync()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _character = await CharacterService.ReloadCharacterAsync(Id, _currentUserId);
        await ResolveDisciplinePowerPoolsAsync();
        StateHasChanged();
    }

    private async Task TryShowRecentBloodlineApprovalToastAsync()
    {
        if (_character == null)
        {
            return;
        }

        var activeBloodline = _character.Bloodlines?.FirstOrDefault(b => b.Status == Data.Models.Enums.BloodlineStatus.Active);
        if (activeBloodline?.ResolvedAt == null)
        {
            return;
        }

        var hoursSinceApproval = (DateTime.UtcNow - activeBloodline.ResolvedAt.Value).TotalHours;
        if (hoursSinceApproval > 24)
        {
            return;
        }

        var key = $"bloodline-approved-{_character.Id}";
        var alreadyShown = await JS.InvokeAsync<string>("sessionStorageGet", (object)key);
        if (!string.IsNullOrEmpty(alreadyShown))
        {
            return;
        }

        var bloodlineName = activeBloodline.BloodlineDefinition?.Name ?? "Bloodline";
        ToastService.Show("Bloodline approved", $"Your bloodline application for {bloodlineName} has been approved!", ToastType.Success);
        await JS.InvokeVoidAsync("sessionStorageSet", (object)key, (object)"1");
    }

    private void HandleBloodlineApproved(int characterId, string bloodlineName)
    {
        _ = InvokeAsync(async () =>
        {
            if (characterId != Id || _character == null || string.IsNullOrEmpty(_currentUserId))
            {
                return;
            }

            _character = await CharacterService.ReloadCharacterAsync(Id, _currentUserId);
            await ResolveDisciplinePowerPoolsAsync();
            StateHasChanged();
        });
    }
}
