using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Helpers;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class StorytellerGlimpse
{
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _accessDenied || string.IsNullOrEmpty(_currentUserId) || _chronicleSubscription != null)
        {
            return;
        }

        _chronicleSubscription = SessionClient.SubscribeChronicleUpdated(HandleChroniclePatch);
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
}
