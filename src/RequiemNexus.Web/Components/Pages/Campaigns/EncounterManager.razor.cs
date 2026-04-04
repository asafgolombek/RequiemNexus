using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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

    private Task OpenNpcPickerForDraftAsync(int encounterId) =>
        OpenNpcPickerAsync(encounterId, isDraft: true, _sourcePicker);

    private Task OpenNpcPickerForActiveAsync(int encounterId) =>
        OpenNpcPickerAsync(encounterId, isDraft: false, _sourcePicker);

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
}
