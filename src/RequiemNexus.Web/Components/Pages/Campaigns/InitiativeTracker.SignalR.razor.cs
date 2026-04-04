using System.Collections.Generic;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private IDisposable? _initiativeSubscription;
    private IDisposable? _characterUpdateSubscription;

    private void RegisterSessionSignalHandlers()
    {
        _initiativeSubscription?.Dispose();
        _characterUpdateSubscription?.Dispose();
        _initiativeSubscription = SessionClient.SubscribeInitiativeUpdated(HandleInitiativeUpdated);
        _characterUpdateSubscription = SessionClient.SubscribeCharacterUpdated(HandleCharacterUpdated);
    }

    private void UnregisterSessionSignalHandlers()
    {
        _initiativeSubscription?.Dispose();
        _initiativeSubscription = null;
        _characterUpdateSubscription?.Dispose();
        _characterUpdateSubscription = null;
    }

    private void HandleInitiativeUpdated(IEnumerable<InitiativeEntryDto> entries)
    {
        _ = InvokeAsync(() => LoadEncounter(showFullPageSpinner: false));
    }

    private void HandleCharacterUpdated(CharacterUpdateDto patch)
    {
        _ = InvokeAsync(async () =>
        {
            await AnnounceConditionDeltasAsync(patch);
            await LoadEncounter(showFullPageSpinner: false);
        });
    }
}
