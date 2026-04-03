using System.Globalization;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Components.UI;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

#pragma warning disable SA1201 // Order of fields, properties, and methods — mirrors generated inject/parameter layout
#pragma warning disable SA1202 // Member order matches original @code block
#pragma warning disable SA1203
#pragma warning disable SA1214
#pragma warning disable SA1503 // Braces in switch arms — preserved from razor @code
#pragma warning disable SA1413 // Trailing comma in diagnostics object
#pragma warning disable SA1513
#pragma warning disable SA1516
#pragma warning disable SA1028

public partial class InitiativeTracker : IAsyncDisposable
{
    [Parameter]
    public int CampaignId { get; set; }

    [Parameter]
    public int EncounterId { get; set; }

    private CombatEncounter? _encounter;
    private bool _accessDenied;
    private List<Character> _campaignCharacters = [];
    private Dictionary<int, List<CharacterTilt>> _activeTilts = [];
    private Dictionary<int, TiltType> _tiltSelections = [];
    private bool _loading = true;
    private bool _isSt;
    private bool _busy;
    private string? _currentUserId;
    private List<InitiativeEntry> SortedEntries =>
        _encounter?.InitiativeEntries.OrderBy(i => i.Order).ToList() ?? [];

    private InitiativeEntry? CurrentActor =>
        _encounter is { ResolvedAt: null } ? SortedEntries.FirstOrDefault(i => !i.HasActed) : null;

    private int? PlayerOwnedCharacterId =>
        _encounter?.InitiativeEntries
            .FirstOrDefault(e => e.CharacterId.HasValue
                && e.Character != null
                && e.Character.ApplicationUserId == _currentUserId)
            ?.CharacterId;

    private string _addType = "character";
    private int _addCharacterId;
    private string _addNpcName = string.Empty;
    private int _addChronicleNpcId;
    private int _addInitMod;
    private int _addRoll = 1;
    private int _addNpcHealthBoxes = 7;
    private int _addNpcMaxWillpower = 4;
    private int _addNpcMaxVitae;
    private bool _addChronicleTracksVitae;
    private List<ChronicleNpc> _chronicleNpcs = [];

    private int? _lastAnnouncedInitiativeEntryId;

    private bool _initiativeAnnouncePrimed;

    private readonly Dictionary<int, HashSet<string>> _conditionNamesByCharacterId = new();

    private IEnumerable<ChronicleNpc> ChronicleNpcsAvailableForEncounter
    {
        get
        {
            if (_encounter?.InitiativeEntries == null)
            {
                return _chronicleNpcs;
            }

            HashSet<int> taken = _encounter.InitiativeEntries
                .Where(i => i.ChronicleNpcId.HasValue)
                .Select(i => i.ChronicleNpcId!.Value)
                .ToHashSet();
            return _chronicleNpcs.Where(n => !taken.Contains(n.Id));
        }
    }
    private int? _defeatConfirmEntryId;
    private int? _npcRollEntryId;
    private bool _meleeAttackModalOpen;
    private int _meleeAttackAttackerCharacterId;
    private bool _playerWeaponDamageModalOpen;
    private int _playerWeaponDamageCharacterId;
    private string _addError = string.Empty;
    private string _actionFeedback = string.Empty;
    private string? _cookieHeader;
    private PersistingComponentStateSubscription _persistingSubscription;
    private int _lastLoadedEncounterId = int.MinValue;
    private int _lastLoadedCampaignId = int.MinValue;
    private int? _hubConnectedCampaignId;

    private readonly CancellationTokenSource _disposeCts = new();
    private CancellationTokenSource? _loadEncounterCts;
    private int _loadGeneration;
    private int _fullPageLoadTicket;

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

    private async Task AnnounceInitiativeTurnIfChangedAsync()
    {
        if (_encounter == null || _accessDenied)
        {
            return;
        }

        InitiativeEntry? actor = CurrentActor;
        if (actor == null)
        {
            return;
        }

        if (!_initiativeAnnouncePrimed)
        {
            _initiativeAnnouncePrimed = true;
            _lastAnnouncedInitiativeEntryId = actor.Id;
            return;
        }

        if (_lastAnnouncedInitiativeEntryId == actor.Id)
        {
            return;
        }

        _lastAnnouncedInitiativeEntryId = actor.Id;
        string label = actor.Character?.Name ?? actor.NpcName ?? actor.MaskedDisplayName ?? "Participant";
        await Announcer.AnnounceAsync($"Initiative order updated — {label} is now active.");
    }

    private async Task AnnounceConditionDeltasAsync(CharacterUpdateDto patch)
    {
        if (patch.ActiveConditions == null)
        {
            return;
        }

        HashSet<string> incoming = patch.ActiveConditions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!_conditionNamesByCharacterId.TryGetValue(patch.CharacterId, out HashSet<string>? prior))
        {
            _conditionNamesByCharacterId[patch.CharacterId] =
                new HashSet<string>(incoming, StringComparer.OrdinalIgnoreCase);
            return;
        }

        foreach (string name in incoming)
        {
            if (!prior.Contains(name))
            {
                await Announcer.AnnounceAsync($"{name} applied to {ResolveCharacterLabel(patch.CharacterId)}");
            }
        }

        _conditionNamesByCharacterId[patch.CharacterId] =
            new HashSet<string>(incoming, StringComparer.OrdinalIgnoreCase);
    }

    private string ResolveCharacterLabel(int characterId)
    {
        if (_encounter?.InitiativeEntries == null)
        {
            return "a character";
        }

        InitiativeEntry? row = _encounter.InitiativeEntries.FirstOrDefault(e => e.CharacterId == characterId);
        return row?.Character?.Name ?? row?.NpcName ?? row?.MaskedDisplayName ?? "a character";
    }

    private void OpenMeleeAttackModal(int attackerCharacterId)
    {
        _meleeAttackAttackerCharacterId = attackerCharacterId;
        _meleeAttackModalOpen = true;
    }

    private async Task OnMeleeAttackModalOpenChanged(bool open)
    {
        _meleeAttackModalOpen = open;
        if (!open)
        {
            await LoadEncounter(showFullPageSpinner: false);
        }
    }

    private void OpenPlayerWeaponDamageModal(int characterId)
    {
        _playerWeaponDamageCharacterId = characterId;
        _playerWeaponDamageModalOpen = true;
    }

    private Task OnPlayerWeaponDamageModalOpenChanged(bool open)
    {
        _playerWeaponDamageModalOpen = open;
        return Task.CompletedTask;
    }

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

    private async Task LoadActiveTiltsAsync()
    {
        if (_encounter == null) return;

        _activeTilts = [];
        _tiltSelections = [];

        foreach (InitiativeEntry entry in _encounter.InitiativeEntries)
        {
            if (!entry.CharacterId.HasValue) continue;

            int charId = entry.CharacterId.Value;
            List<CharacterTilt> tilts = await ConditionService.GetActiveTiltsAsync(charId);
            _activeTilts[charId] = tilts;
            _tiltSelections.TryAdd(charId, TiltType.KnockedDown);
        }
    }

    private bool CanAddParticipant()
    {
        return _addType switch
        {
            "character" => _addCharacterId > 0,
            "npc" => !string.IsNullOrWhiteSpace(_addNpcName),
            "chronicle" => _addChronicleNpcId > 0 && ChronicleNpcsAvailableForEncounter.Any(),
            _ => false,
        };
    }

    private void OnAddTypeChanged()
    {
        _addError = string.Empty;
        _addChronicleNpcId = 0;
        _addNpcMaxVitae = 0;
        _addChronicleTracksVitae = false;
    }

    private async Task OnTrackerChronicleSelectChanged(ChangeEventArgs e)
    {
        _addError = string.Empty;
        string? raw = e.Value?.ToString();
        _addChronicleNpcId = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            ? id
            : 0;

        if (_addChronicleNpcId == 0 || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        ChronicleNpcEncounterPrepDto? prep =
            await EncounterPrepService.GetChronicleNpcEncounterPrepAsync(_addChronicleNpcId, _currentUserId);
        if (prep != null)
        {
            _addInitMod = prep.SuggestedInitiativeMod;
            _addNpcHealthBoxes = prep.SuggestedHealthBoxes;
            _addNpcMaxWillpower = prep.SuggestedMaxWillpower;
            _addChronicleTracksVitae = prep.TracksVitae;
            _addNpcMaxVitae = prep.TracksVitae ? prep.SuggestedMaxVitae : 0;
        }
    }

    private async Task AddParticipant()
    {
        if (!CanAddParticipant() || _busy) return;

        _busy = true;
        _addError = string.Empty;
        try
        {
            if (_addType == "character")
            {
                await EncounterParticipantService.AddCharacterToEncounterAsync(EncounterId, _addCharacterId, _addInitMod, _addRoll, _currentUserId!);
            }
            else if (_addType == "npc")
            {
                await EncounterParticipantService.AddNpcToEncounterAsync(
                    EncounterId,
                    _addNpcName.Trim(),
                    _addInitMod,
                    _addRoll,
                    _currentUserId!,
                    _addNpcHealthBoxes,
                    _addNpcMaxWillpower);
            }
            else
            {
                await EncounterParticipantService.AddNpcToEncounterFromChronicleNpcAsync(
                    EncounterId,
                    _addChronicleNpcId,
                    _addInitMod,
                    _addRoll,
                    _addNpcHealthBoxes,
                    _addNpcMaxWillpower,
                    _addNpcMaxVitae,
                    _currentUserId!);
            }

            _addCharacterId = 0;
            _addNpcName = string.Empty;
            _addChronicleNpcId = 0;
            _addInitMod = 0;
            _addRoll = 1;
            _addNpcHealthBoxes = 7;
            _addNpcMaxWillpower = 4;
            _addNpcMaxVitae = 0;
            _addChronicleTracksVitae = false;

            await LoadEncounter();
        }
        catch (Exception ex)
        {
            _addError = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private void CancelDefeatConfirm() => _defeatConfirmEntryId = null;

    private async Task ConfirmDefeatNpc(int entryId)
    {
        _defeatConfirmEntryId = null;
        await RemoveEntry(entryId);
    }

    private async Task AdvanceTurn()
    {
        if (_busy) return;

        _busy = true;
        _actionFeedback = string.Empty;
        try
        {
            await EncounterService.AdvanceTurnAsync(EncounterId, _currentUserId!);
            await LoadEncounter();

            if (SortedEntries.All(i => !i.HasActed))
            {
                _actionFeedback = $"Round {_encounter?.CurrentRound ?? 1}";
            }
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task HoldTurn()
    {
        if (_busy) return;

        _busy = true;
        try
        {
            await EncounterService.HoldActionAsync(EncounterId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ReleaseHeld(int entryId)
    {
        if (_busy) return;

        _busy = true;
        try
        {
            await EncounterService.ReleaseHeldActionAsync(EncounterId, entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private bool CanStEditLaunchedNpcEncounter() =>
        _encounter != null
        && !_encounter.IsDraft
        && _encounter.ResolvedAt == null
        && (_encounter.IsActive || _encounter.IsPaused);

    private async Task OnNpcHealthTrackChangedAsync(int entryId, string track)
    {
        if (_busy || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        _actionFeedback = string.Empty;
        try
        {
            await NpcCombatService.SetNpcHealthDamageAsync(entryId, track, _currentUserId);
            await LoadEncounter(showFullPageSpinner: false);
        }
        catch (Exception ex)
        {
            _actionFeedback = ex.Message;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task NpcSpendWill(int entryId)
    {
        if (_busy) return;

        _busy = true;
        try
        {
            await NpcCombatService.SpendNpcWillpowerAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task NpcRestoreWill(int entryId)
    {
        if (_busy) return;

        _busy = true;
        try
        {
            await NpcCombatService.RestoreNpcWillpowerAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task NpcSpendVitae(int entryId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await NpcCombatService.SpendNpcVitaeAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task NpcRestoreVitae(int entryId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await NpcCombatService.RestoreNpcVitaeAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private void ToggleNpcRoll(int entryId)
    {
        _npcRollEntryId = _npcRollEntryId == entryId ? null : entryId;
    }

    private Task CloseNpcRollPanelAsync()
    {
        _npcRollEntryId = null;
        return Task.CompletedTask;
    }

    private async Task ToggleNpcReveal(InitiativeEntry entry)
    {
        if (_busy) return;

        _busy = true;
        try
        {
            await NpcCombatService.SetNpcEntryRevealAsync(entry.Id, !entry.IsRevealed, entry.MaskedDisplayName, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ResolveEncounter()
    {
        if (_busy) return;

        _busy = true;
        try
        {
            await EncounterService.ResolveEncounterAsync(EncounterId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RemoveEntry(int entryId)
    {
        if (_busy) return;

        _busy = true;
        try
        {
            await EncounterParticipantService.RemoveEntryAsync(entryId, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ApplyTilt(int characterId)
    {
        if (_busy || !_tiltSelections.TryGetValue(characterId, out TiltType tiltType)) return;

        _busy = true;
        try
        {
            await ConditionService.ApplyTiltAsync(characterId, tiltType, null, EncounterId, _currentUserId!);
            await LoadActiveTiltsAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RemoveTilt(int tiltId)
    {
        if (_busy) return;

        _busy = true;
        try
        {
            await ConditionService.RemoveTiltAsync(tiltId, _currentUserId!);
            await LoadActiveTiltsAsync();
        }
        finally
        {
            _busy = false;
        }
    }

    private void OnDragStart(InitiativeEntry item) => InitiativeTrackerDragState.SetDraggedItem(item);

    private async Task OnDrop(InitiativeEntry target)
    {
        InitiativeEntry? dragged = InitiativeTrackerDragState.DraggedItem;
        if (dragged is null || dragged == target || _encounter == null)
        {
            return;
        }

        List<InitiativeEntry> list = _encounter.InitiativeEntries.OrderBy(i => i.Order).ToList();
        int oldIdx = list.IndexOf(dragged);
        int newIdx = list.IndexOf(target);

        list.RemoveAt(oldIdx);
        list.Insert(newIdx, dragged);

        List<int> orderIds = list.Select(e => e.Id).ToList();
        InitiativeTrackerDragState.ClearDrag();

        _busy = true;
        try
        {
            await EncounterService.ReorderInitiativeAsync(EncounterId, orderIds, _currentUserId!);
            await LoadEncounter();
        }
        finally
        {
            _busy = false;
        }
    }

    private bool ShowPlayerHealth(InitiativeEntry entry) =>
        entry.Character != null && _isSt;

    private string GetDamageClass(Character character, int index)
    {
        if (string.IsNullOrEmpty(character.HealthDamage) || index >= character.HealthDamage.Length)
        {
            return string.Empty;
        }

        return character.HealthDamage[index] switch
        {
            '/' => "bashing",
            'X' => "lethal",
            '*' => "aggravated",
            _ => string.Empty
        };
    }

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
