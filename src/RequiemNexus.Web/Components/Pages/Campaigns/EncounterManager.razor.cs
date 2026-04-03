using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

#pragma warning disable SA1201 // Order of fields, properties, and methods — mirrors generated inject/parameter layout
#pragma warning disable SA1203 // Constant field order grouped with picker mode for readability
#pragma warning disable SA1214 // Readonly field order — dictionary kept with related launch state

public partial class EncounterManager
{
    [Parameter]
    public int Id { get; set; }

    private List<CombatEncounter> _encounters = [];
    private List<NpcStatBlock> _availableBlocks = [];
    private List<EncounterTemplate> _templates = [];
    private List<Character> _campaignCharacters = [];
    private readonly Dictionary<int, bool> _smartLaunchSelection = [];
    private int? _smartLaunchEncounterId;
    private bool _smartLaunchIsPrepStart;
    private string _prepFeedback = string.Empty;

    private bool _loading = true;
    private bool _accessDenied;
    private bool _busy;
    private string? _currentUserId;
    private string _createError = string.Empty;
    private bool _showCreateEncounterForm;
    private string _formEncounterName = string.Empty;
    private string _formEncounterNotes = string.Empty;
    private int? _formDraftEncounterId;
    private int? _activeEncounterForNpc;
    private bool _npcPickerIsDraft;
    private int _selectedBlockId;
    private int _npcInitMod;
    private int _npcRoll = 1;
    private int _selectedTemplateId;
    private string _fromTemplateEncounterName = string.Empty;

    private const string _sourcePicker = "source";
    private const string _statPicker = "stat";
    private const string _chroniclePicker = "chronicle";
    private const string _improvisedPicker = "improv";
    private string? _npcPickerMode;
    private List<ChronicleNpc> _chronicleNpcs = [];
    private HashSet<int> _excludedChronicleNpcIds = [];
    private int _selectedChronicleNpcId;
    private int _chronicleHealthBoxes = 7;
    private int _chronicleMaxWillpower = 4;
    private int _chronicleMaxVitae;
    private bool _chronicleTracksVitae;
    private string? _chroniclePrepHint;
    private string _chronicleAddError = string.Empty;
    private string _improvName = string.Empty;
    private int _improvHealthBoxes = 7;
    private int _improvMaxWillpower = 4;
    private string _improvError = string.Empty;
    private int? _renamingEncounterId;
    private string _renameEncounterBuffer = string.Empty;

    private List<CombatEncounter> DraftEncounters => _encounters.Where(e => e.IsDraft).ToList();

    private IEnumerable<CombatEncounter> DraftEncountersForList =>
        DraftEncounters.Where(e => !_showCreateEncounterForm || _formDraftEncounterId != e.Id);

    private CombatEncounter? FormDraftEncounter =>
        _formDraftEncounterId is int formId ? _encounters.FirstOrDefault(e => e.Id == formId) : null;

    private List<CombatEncounter> LaunchedEncounters => _encounters.Where(e => e.IsActive && !e.IsDraft).ToList();

    private List<CombatEncounter> PausedEncounters => _encounters.Where(e => e.IsPaused && !e.IsDraft && e.ResolvedAt == null).ToList();

    private List<CombatEncounter> PastEncounters => _encounters.Where(e => e.ResolvedAt != null).ToList();

    protected override async Task OnInitializedAsync()
    {
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(_currentUserId))
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        await LoadEncounters();
    }

    private bool GetSmartCheck(int characterId) =>
        _smartLaunchSelection.GetValueOrDefault(characterId, true);

    private void SetSmartCheck(int characterId, bool value) => _smartLaunchSelection[characterId] = value;

    private Task OnSmartLaunchSelectionChangedAsync((int CharacterId, bool Selected) args)
    {
        SetSmartCheck(args.CharacterId, args.Selected);
        return Task.CompletedTask;
    }

    private Task CancelSmartLaunchAsync()
    {
        CancelSmartLaunch();
        return Task.CompletedTask;
    }

    private Task OnDraftPrepStartRenameAsync(CombatEncounter enc)
    {
        StartEncounterRename(enc);
        return Task.CompletedTask;
    }

    private Task CancelEncounterRenameAsync()
    {
        CancelEncounterRename();
        return Task.CompletedTask;
    }

    private Task AckSaveForLaterAsync()
    {
        AckSaveForLater();
        return Task.CompletedTask;
    }

    private Task OpenSmartLaunchPrepAsync(int encounterId)
    {
        OpenSmartLaunch(encounterId, prepStart: true);
        return Task.CompletedTask;
    }

    private Task OpenNpcPickerForDraftAsync(int encounterId) =>
        OpenNpcPickerAsync(encounterId, isDraft: true, _sourcePicker);

    private Task OpenNpcPickerForActiveAsync(int encounterId) =>
        OpenNpcPickerAsync(encounterId, isDraft: false, _sourcePicker);

    private Task OpenSmartLaunchForActiveAsync(int encounterId)
    {
        OpenSmartLaunch(encounterId, prepStart: false);
        return Task.CompletedTask;
    }

    private async Task LoadEncounters()
    {
        _loading = true;
        try
        {
            Campaign? campaign = await CampaignService.GetCampaignByIdAsync(Id, _currentUserId!);
            if (campaign == null || !CampaignService.IsStoryteller(campaign, _currentUserId!))
            {
                _accessDenied = true;
                return;
            }

            _encounters = await EncounterQueryService.GetEncountersAsync(Id, _currentUserId!);

            _availableBlocks = await NpcStatBlockService.GetAvailableBlocksAsync(Id);
            _chronicleNpcs = await ChronicleNpcService.GetNpcsAsync(Id, includeDeceased: false);
            _templates = await EncounterTemplateService.GetTemplatesAsync(Id, _currentUserId!);
            _campaignCharacters = campaign.Characters.Where(c => !c.IsRetired).ToList();
            _accessDenied = false;
        }
        finally
        {
            _loading = false;
        }
    }

    private void OpenCreateEncounterForm()
    {
        _showCreateEncounterForm = true;
        _formEncounterName = string.Empty;
        _formEncounterNotes = string.Empty;
        _formDraftEncounterId = null;
        _createError = string.Empty;
    }

    private void CloseCreateEncounterForm()
    {
        _showCreateEncounterForm = false;
        _formEncounterName = string.Empty;
        _formEncounterNotes = string.Empty;
        _formDraftEncounterId = null;
        _createError = string.Empty;
    }

    private async Task<bool> EnsureDraftForFormAsync()
    {
        if (string.IsNullOrWhiteSpace(_formEncounterName))
        {
            _createError = "Encounter name is required.";
            return false;
        }

        if (_formDraftEncounterId.HasValue)
        {
            return true;
        }

        _busy = true;
        _createError = string.Empty;
        try
        {
            CombatEncounter enc = await EncounterPrepService.CreateDraftEncounterAsync(
                Id,
                _formEncounterName.Trim(),
                _currentUserId!,
                _formEncounterNotes);
            _formDraftEncounterId = enc.Id;
            await LoadEncounters();
            return true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Encounter", ex.Message, ToastType.Error);
            return false;
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task PersistFormDraftAsync(int encounterId)
    {
        await EncounterPrepService.UpdateDraftEncounterNameAsync(encounterId, _formEncounterName.Trim(), _currentUserId!);
        await EncounterPrepService.UpdateDraftEncounterPrepNotesAsync(encounterId, _formEncounterNotes, _currentUserId!);
        await LoadEncounters();
    }

    private async Task OpenAddNpcForCreateFormAsync()
    {
        if (!await EnsureDraftForFormAsync())
        {
            return;
        }

        await OpenNpcPickerAsync(_formDraftEncounterId!.Value, isDraft: true, _sourcePicker);
    }

    private async Task CreateFormSaveForLaterAsync()
    {
        if (!await EnsureDraftForFormAsync())
        {
            return;
        }

        int encId = _formDraftEncounterId!.Value;
        await PersistFormDraftAsync(encId);
        AckSaveForLater();
        CloseCreateEncounterForm();
    }

    private async Task CreateFormStartNowAsync()
    {
        if (!await EnsureDraftForFormAsync())
        {
            return;
        }

        int encId = _formDraftEncounterId!.Value;
        await PersistFormDraftAsync(encId);
        CloseCreateEncounterForm();
        OpenSmartLaunch(encId, prepStart: true);
    }

    private async Task DiscardCreateEncounterFormAsync()
    {
        if (_formDraftEncounterId.HasValue)
        {
            _busy = true;
            _createError = string.Empty;
            try
            {
                await EncounterPrepService.DeleteDraftEncounterAsync(_formDraftEncounterId.Value, _currentUserId!);
                await LoadEncounters();
            }
            catch (Exception ex)
            {
                ToastService.Show("Encounter", ex.Message, ToastType.Error);
                return;
            }
            finally
            {
                _busy = false;
            }
        }

        CancelSmartLaunch();
        CloseCreateEncounterForm();
    }

    private async Task CreateFromTemplate()
    {
        if (_selectedTemplateId == 0 || _busy)
        {
            return;
        }

        string name = string.IsNullOrWhiteSpace(_fromTemplateEncounterName) ? "Encounter" : _fromTemplateEncounterName.Trim();
        _busy = true;
        try
        {
            await EncounterTemplateService.CreateDraftEncounterFromTemplateAsync(_selectedTemplateId, name, _currentUserId!);
            _fromTemplateEncounterName = string.Empty;
            _selectedTemplateId = 0;
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task RemoveTemplateRow(int templateId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterPrepService.RemoveNpcTemplateAsync(templateId, _currentUserId!);
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }

    private void AckSaveForLater()
    {
        _prepFeedback = "Saved. This encounter stays in your list until you use Start.";
    }

    private async Task OpenNpcPickerAsync(int encounterId, bool isDraft, string mode)
    {
        _activeEncounterForNpc = encounterId;
        _npcPickerIsDraft = isDraft;
        _npcPickerMode = mode;
        _selectedBlockId = 0;
        _selectedChronicleNpcId = 0;
        _npcInitMod = 0;
        _npcRoll = 1;
        _chronicleHealthBoxes = 7;
        _chronicleMaxWillpower = 4;
        _chronicleMaxVitae = 0;
        _chronicleTracksVitae = false;
        _chroniclePrepHint = null;
        _chronicleAddError = string.Empty;
        _improvName = string.Empty;
        _improvHealthBoxes = 7;
        _improvMaxWillpower = 4;
        _improvError = string.Empty;
        _excludedChronicleNpcIds.Clear();

        if (isDraft)
        {
            CombatEncounter? draftEnc = _encounters.FirstOrDefault(e => e.Id == encounterId);
            if (draftEnc?.NpcTemplates != null)
            {
                foreach (EncounterNpcTemplate t in draftEnc.NpcTemplates)
                {
                    if (t.ChronicleNpcId is int cid)
                    {
                        _ = _excludedChronicleNpcIds.Add(cid);
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(_currentUserId))
        {
            CombatEncounter? live = await EncounterQueryService.GetEncounterAsync(encounterId, _currentUserId);
            if (live?.NpcTemplates != null)
            {
                foreach (EncounterNpcTemplate t in live.NpcTemplates)
                {
                    if (t.ChronicleNpcId is int cid)
                    {
                        _ = _excludedChronicleNpcIds.Add(cid);
                    }
                }
            }

            if (live?.InitiativeEntries != null)
            {
                foreach (InitiativeEntry row in live.InitiativeEntries)
                {
                    if (row.ChronicleNpcId is int iid)
                    {
                        _ = _excludedChronicleNpcIds.Add(iid);
                    }
                }
            }
        }
    }

    private void CloseNpcPicker()
    {
        _activeEncounterForNpc = null;
        _npcPickerMode = null;
        _chroniclePrepHint = null;
        _chronicleAddError = string.Empty;
        _improvError = string.Empty;
    }

    private Task CloseNpcPickerAsync()
    {
        CloseNpcPicker();
        return Task.CompletedTask;
    }

    private void OnNpcModalKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            CloseNpcPicker();
        }
    }

    private Task OnNpcModalKeyDownAsync(KeyboardEventArgs e)
    {
        OnNpcModalKeyDown(e);
        return Task.CompletedTask;
    }

    private string GetNpcModalTitle() =>
        _npcPickerMode switch
        {
            _sourcePicker => "Add NPC",
            _statPicker => "Add NPC — stat block",
            _chroniclePicker => "Add NPC — Danse Macabre",
            _improvisedPicker => "Add NPC — Improvised",
            _ => "Add NPC",
        };

    private void StartEncounterRename(CombatEncounter enc)
    {
        _renamingEncounterId = enc.Id;
        _renameEncounterBuffer = enc.Name;
    }

    private void CancelEncounterRename()
    {
        _renamingEncounterId = null;
        _renameEncounterBuffer = string.Empty;
    }

    private async Task SaveEncounterRename(int encounterId)
    {
        if (_busy || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterPrepService.UpdateDraftEncounterNameAsync(encounterId, _renameEncounterBuffer, _currentUserId);
            CancelEncounterRename();
            await LoadEncounters();
        }
        catch (Exception ex)
        {
            ToastService.Show("Encounter", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OnChronicleNpcSelectChanged(ChangeEventArgs e)
    {
        _chronicleAddError = string.Empty;
        string? raw = e.Value?.ToString();
        _selectedChronicleNpcId = int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)
            ? id
            : 0;
        await RefreshChroniclePrepAsync();
    }

    private async Task RefreshChroniclePrepAsync()
    {
        _chroniclePrepHint = null;
        if (_selectedChronicleNpcId == 0 || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        ChronicleNpcEncounterPrepDto? prep =
            await EncounterPrepService.GetChronicleNpcEncounterPrepAsync(_selectedChronicleNpcId, _currentUserId);
        if (prep == null)
        {
            return;
        }

        _npcInitMod = prep.SuggestedInitiativeMod;
        _chronicleHealthBoxes = prep.SuggestedHealthBoxes;
        _chronicleMaxWillpower = prep.SuggestedMaxWillpower;
        _chronicleTracksVitae = prep.TracksVitae;
        _chronicleMaxVitae = prep.TracksVitae ? prep.SuggestedMaxVitae : 0;
        string vitaeHint = prep.TracksVitae ? $", vitae {prep.SuggestedMaxVitae} (Blood Potency)." : string.Empty;
        _chroniclePrepHint = string.IsNullOrEmpty(prep.LinkedStatBlockName)
            ? $"Suggested from sheet: mod {prep.SuggestedInitiativeMod} (Wits + Composure), health {prep.SuggestedHealthBoxes}, willpower {prep.SuggestedMaxWillpower} (Resolve + Composure){vitaeHint}"
            : $"Linked stat block \"{prep.LinkedStatBlockName}\": mod {prep.SuggestedInitiativeMod}, health {prep.SuggestedHealthBoxes}, willpower {prep.SuggestedMaxWillpower}{vitaeHint}";
    }

    private async Task AddNpcFromChronicle()
    {
        _chronicleAddError = string.Empty;
        if (_selectedChronicleNpcId == 0)
        {
            _chronicleAddError = "Select a chronicle NPC from the list.";
            return;
        }

        if (!_activeEncounterForNpc.HasValue || _busy || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _busy = true;
        try
        {
            if (_npcPickerIsDraft)
            {
                await EncounterPrepService.AddNpcTemplateFromChronicleNpcAsync(
                    _activeEncounterForNpc.Value,
                    _selectedChronicleNpcId,
                    _npcInitMod,
                    _chronicleHealthBoxes,
                    _chronicleMaxWillpower,
                    _chronicleMaxVitae,
                    isRevealed: true,
                    defaultMaskedName: null,
                    storyTellerUserId: _currentUserId);
            }
            else
            {
                await EncounterParticipantService.AddNpcToEncounterFromChronicleNpcAsync(
                    _activeEncounterForNpc.Value,
                    _selectedChronicleNpcId,
                    _npcInitMod,
                    _npcRoll,
                    _chronicleHealthBoxes,
                    _chronicleMaxWillpower,
                    _chronicleMaxVitae,
                    _currentUserId);
            }

            CloseNpcPicker();
            await LoadEncounters();
        }
        catch (Exception ex)
        {
            ToastService.Show("Encounter", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task AddNpcFromStatBlock()
    {
        if (_selectedBlockId == 0 || !_activeEncounterForNpc.HasValue || _busy)
        {
            return;
        }

        _busy = true;
        try
        {
            NpcStatBlock? block = await NpcStatBlockService.GetBlockAsync(_selectedBlockId);
            if (block is null)
            {
                return;
            }

            int hp = Math.Max(1, block.Health);
            int wp = Math.Max(1, block.Willpower);

            if (_npcPickerIsDraft)
            {
                await EncounterPrepService.AddNpcTemplateAsync(
                    _activeEncounterForNpc.Value,
                    block.Name,
                    _npcInitMod,
                    hp,
                    wp,
                    null,
                    true,
                    null,
                    _currentUserId!);
            }
            else
            {
                await EncounterParticipantService.AddNpcToEncounterAsync(
                    _activeEncounterForNpc.Value,
                    block.Name,
                    _npcInitMod,
                    _npcRoll,
                    _currentUserId!,
                    hp,
                    wp);
            }

            CloseNpcPicker();
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task AddImprovisedNpc()
    {
        if (!_activeEncounterForNpc.HasValue || _busy || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _improvError = string.Empty;
        if (string.IsNullOrWhiteSpace(_improvName))
        {
            _improvError = "Name is required.";
            return;
        }

        if (_improvHealthBoxes < 1 || _improvHealthBoxes > 50)
        {
            _improvError = "Health must be between 1 and 50.";
            return;
        }

        if (_improvMaxWillpower < 1 || _improvMaxWillpower > 20)
        {
            _improvError = "Willpower must be between 1 and 20.";
            return;
        }

        _busy = true;
        try
        {
            await EncounterPrepService.AddNpcTemplateAsync(
                _activeEncounterForNpc.Value,
                _improvName.Trim(),
                0,
                _improvHealthBoxes,
                _improvMaxWillpower,
                null,
                true,
                null,
                _currentUserId);
            CloseNpcPicker();
            await LoadEncounters();
        }
        catch (Exception ex)
        {
            ToastService.Show("Encounter", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ResolveEncounter(int encounterId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.ResolveEncounterAsync(encounterId, _currentUserId!);
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }

    private void OpenSmartLaunch(int encounterId, bool prepStart)
    {
        _smartLaunchEncounterId = encounterId;
        _smartLaunchIsPrepStart = prepStart;
        _createError = string.Empty;
        foreach (Character ch in _campaignCharacters)
        {
            _smartLaunchSelection[ch.Id] = true;
        }
    }

    private void CancelSmartLaunch()
    {
        _smartLaunchEncounterId = null;
        _smartLaunchIsPrepStart = false;
    }

    private async Task ConfirmSmartLaunch()
    {
        if (!_smartLaunchEncounterId.HasValue || _busy)
        {
            return;
        }

        bool startedFromPrep = _smartLaunchIsPrepStart;
        int encounterId = _smartLaunchEncounterId.Value;

        List<int> ids = _campaignCharacters
            .Where(c => _smartLaunchSelection.GetValueOrDefault(c.Id, false))
            .Select(c => c.Id)
            .ToList();

        _busy = true;
        _createError = string.Empty;
        try
        {
            if (startedFromPrep)
            {
                await EncounterService.LaunchEncounterAsync(encounterId, _currentUserId!);
            }

            await EncounterParticipantService.BulkAddOnlinePlayersAsync(encounterId, ids, _currentUserId!);

            CancelSmartLaunch();
            await LoadEncounters();

            if (startedFromPrep)
            {
                NavigationManager.NavigateTo($"/campaigns/{Id}/encounter/{encounterId}", forceLoad: true);
            }
        }
        catch (Exception ex)
        {
            await LoadEncounters();
            ToastService.Show("Encounter", ex.Message, ToastType.Error);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task PauseEncounter(int encounterId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.PauseEncounterAsync(encounterId, _currentUserId!);
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task ResumeEncounter(int encounterId)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        try
        {
            await EncounterService.ResumeEncounterAsync(encounterId, _currentUserId!);
            await LoadEncounters();
        }
        finally
        {
            _busy = false;
        }
    }
}
