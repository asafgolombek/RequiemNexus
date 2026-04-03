using System.Collections.Generic;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.RealTime;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    private void RegisterSessionSignalHandlers()
    {
        SessionClient.InitiativeUpdated += HandleInitiativeUpdated;
        SessionClient.CharacterUpdated += HandleCharacterUpdated;
    }

    private void UnregisterSessionSignalHandlers()
    {
        SessionClient.InitiativeUpdated -= HandleInitiativeUpdated;
        SessionClient.CharacterUpdated -= HandleCharacterUpdated;
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
