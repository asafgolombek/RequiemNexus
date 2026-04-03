using System.Threading;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

public partial class InitiativeTracker
{
    /// <param name="showFullPageSpinner">When false, refreshes data without replacing the UI with the loading message (used for SignalR and in-place actions).</param>
    private async Task LoadEncounter(bool showFullPageSpinner = false)
    {
        int myGeneration = Interlocked.Increment(ref _loadGeneration);
        _loadEncounterCts?.Cancel();
        _loadEncounterCts?.Dispose();
        try
        {
            _loadEncounterCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        CancellationToken ct = _loadEncounterCts.Token;

        if (showFullPageSpinner)
        {
            _loading = true;
            Volatile.Write(ref _fullPageLoadTicket, myGeneration);
        }

        try
        {
            ct.ThrowIfCancellationRequested();

            if (EncounterId != _lastLoadedEncounterId)
            {
                _conditionNamesByCharacterId.Clear();
                _lastAnnouncedInitiativeEntryId = null;
                _initiativeAnnouncePrimed = false;
            }

            _accessDenied = false;
            try
            {
                _encounter = await EncounterQueryService.GetEncounterAsync(EncounterId, _currentUserId!);
            }
            catch (UnauthorizedAccessException)
            {
                _encounter = null;
                _accessDenied = true;
                return;
            }

            if (_encounter == null)
            {
                return;
            }

            ct.ThrowIfCancellationRequested();

            Campaign? campaign = await CampaignService.GetCampaignByIdAsync(CampaignId, _currentUserId!);
            _isSt = campaign != null && CampaignService.IsStoryteller(campaign, _currentUserId!);

            if (_isSt && campaign != null)
            {
                _campaignCharacters = campaign.Characters.ToList();
                _chronicleNpcs = await ChronicleNpcService.GetNpcsAsync(CampaignId, includeDeceased: false);
            }
            else
            {
                _chronicleNpcs = [];
            }

            ct.ThrowIfCancellationRequested();

            await LoadActiveTiltsAsync();
            await AnnounceInitiativeTurnIfChangedAsync();

            if (myGeneration != Volatile.Read(ref _loadGeneration))
            {
                return;
            }

            StateHasChanged();
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (showFullPageSpinner && Volatile.Read(ref _fullPageLoadTicket) == myGeneration)
            {
                _loading = false;
            }

            if (myGeneration == Volatile.Read(ref _loadGeneration))
            {
                _lastLoadedEncounterId = EncounterId;
                _lastLoadedCampaignId = CampaignId;
            }
        }
    }
}
