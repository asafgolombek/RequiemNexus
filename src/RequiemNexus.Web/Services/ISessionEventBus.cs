using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Web.Services;

/// <summary>
/// Token-based subscriptions to <see cref="SessionClientService"/> hub broadcasts.
/// Prefer <c>Subscribe*</c> + a single <see cref="IDisposable.Dispose"/> over multiple <c>event +=</c> / <c>-=</c> pairs.
/// </summary>
/// <remarks>
/// Handlers may run on the SignalR callback thread; UI components should marshal to the renderer via
/// <see cref="Microsoft.AspNetCore.Components.ComponentBase.InvokeAsync"/> before calling <c>StateHasChanged</c>.
/// </remarks>
public interface ISessionEventBus
{
    IDisposable SubscribePresenceUpdated(Action<IEnumerable<PlayerPresenceDto>> handler);

    IDisposable SubscribeDiceRollReceived(Action<DiceRollResultDto> handler);

    IDisposable SubscribeRollHistoryReceived(Action<IEnumerable<DiceRollResultDto>> handler);

    IDisposable SubscribeCharacterUpdated(Action<CharacterUpdateDto> handler);

    IDisposable SubscribeBloodlineApproved(Action<int, string> handler);

    IDisposable SubscribeInitiativeUpdated(Action<IEnumerable<InitiativeEntryDto>> handler);

    IDisposable SubscribeConditionNotificationReceived(Action<ConditionNotificationDto> handler);

    IDisposable SubscribeChronicleUpdated(Action<ChronicleUpdateDto> handler);

    IDisposable SubscribeSocialManeuverUpdated(Action<SocialManeuverUpdateDto> handler);

    IDisposable SubscribeRelationshipUpdated(Action<RelationshipUpdateDto> handler);

    IDisposable SubscribeSessionStarted(Action handler);

    IDisposable SubscribeSessionEnded(Action<string> handler);
}
