// Blazor code-behind: member ordering follows handler grouping rather than StyleCop element order.
#pragma warning disable SA1201 // Order of fields, properties, and methods
#pragma warning disable SA1202 // Public/protected/private ordering
#pragma warning disable SA1204 // Static before instance
#pragma warning disable SA1214 // Readonly fields before non-readonly

using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using RequiemNexus.Web.Components.UI;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages;

/// <summary>
/// Code-behind for the interactive character sheet page.
/// </summary>
public partial class CharacterDetails : IAsyncDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>Route parameter: character id.</summary>
    [Parameter]
    public int Id { get; set; }

    private Character? _character;
    private string? _currentUserId;
    private string? _cookieHeader;
    private List<Equipment> _availableEquipment = new List<Equipment>();
    private int _selectedEquipmentId = 0;
    private int _selectedEquipmentQuantity = 1;

    private bool _isRollerOpen = false;
    private string _rollerTraitName = string.Empty;
    private int _rollerBaseDice = 1;
    private bool _isApplyBloodlineModalOpen = false;
    private bool _removingBloodline = false;
    private List<BloodlineSummaryDto> _eligibleBloodlines = [];
    private bool _isApplyCovenantModalOpen = false;
    private bool _isApplyLearnRiteModalOpen = false;
    private bool _openingRiteModal = false;
    private List<SorceryRiteSummaryDto> _eligibleRites = [];
    private bool _isChosenMysteryModalOpen = false;
    private bool _isLearnCoilModalOpen = false;
    private List<ScaleSummaryDto> _eligibleScales = [];
    private List<CoilSummaryDto> _eligibleCoils = [];
    private bool _requestingLeave = false;
    private List<CovenantSummaryDto> _eligibleCovenants = [];
    private HashSet<int> _expandedMeritIds = [];

    private bool _isAdvancementMode = false;
    private bool _isFreeEditMode = false;
    private string _activeTab = "sheet";
    private bool _isEditingName = false;
    private bool _isExporting = false;

    private readonly HashSet<int> _expandedDisciplines = new HashSet<int>();

    /// <summary>True when the character may use the Blood Sorcery sheet section (Crúac/Theban covenant, Ordo rituals, or Necromancy dots).</summary>
    private bool ShowBloodSorcerySection =>
        _character != null
        && _character.CovenantJoinStatus == null
        && (
            (_character.Covenant?.SupportsBloodSorcery == true)
            || (_character.Covenant?.SupportsOrdoRituals == true)
            || _character.GetDisciplineRating("Necromancy") > 0);

    private bool IsOrdoDraculMember =>
        _character?.Covenant?.Name == "The Ordo Dracul"
        && _character.CovenantJoinStatus == null;

    private void ToggleDiscipline(int disciplineId)
    {
        if (_expandedDisciplines.Contains(disciplineId))
        {
            _expandedDisciplines.Remove(disciplineId);
        }
        else
        {
            _expandedDisciplines.Add(disciplineId);
        }
    }

    private void SelectTab(string tab) => _activeTab = tab;

    private void HandleTabKeydown(KeyboardEventArgs e)
    {
        var tabs = new[] { "sheet", "identity", "history" };
        int i = Array.IndexOf(tabs, _activeTab);
        if (i < 0)
        {
            return;
        }

        if (e.Key == "ArrowRight" || e.Key == "ArrowDown")
        {
            SelectTab(tabs[(i + 1) % tabs.Length]);
        }
        else if (e.Key == "ArrowLeft" || e.Key == "ArrowUp")
        {
            SelectTab(tabs[(i - 1 + tabs.Length) % tabs.Length]);
        }
        else if (e.Key == "Home")
        {
            SelectTab(tabs[0]);
        }
        else if (e.Key == "End")
        {
            SelectTab(tabs[^1]);
        }
    }

    private async Task ExportJson()
    {
        if (_character == null)
        {
            return;
        }

        _isExporting = true;
        try
        {
            var json = ExportService.ExportCharacterAsJson(_character);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            await JS.InvokeVoidAsync("downloadFileFromBase64", $"{_character.Name}.json", "application/json", base64);
            ToastService.Show("Export complete", "JSON downloaded.", ToastType.Success);
        }
        finally
        {
            _isExporting = false;
        }
    }

    private async Task ExportPdf()
    {
        if (_character == null)
        {
            return;
        }

        _isExporting = true;
        try
        {
            var pdfBytes = ExportService.ExportCharacterAsPdf(_character);
            var base64 = Convert.ToBase64String(pdfBytes);
            await JS.InvokeVoidAsync("downloadFileFromBase64", $"{_character.Name}.pdf", "application/pdf", base64);
            ToastService.Show("Export complete", "PDF downloaded.", ToastType.Success);
        }
        finally
        {
            _isExporting = false;
        }
    }

    private void GoToAdvancement()
    {
        NavigationManager.NavigateTo($"/character/{Id}/advancement");
    }

    private async Task SaveNameOffBlur()
    {
        _isEditingName = false;
        await SaveCharacter();
    }

    private async Task HandleNameKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await SaveNameOffBlur();
        }
    }

    private async Task ToggleFreeEditMode()
    {
        bool wasEditing = _isFreeEditMode;
        _isFreeEditMode = !_isFreeEditMode;
        if (_isFreeEditMode)
        {
            _isAdvancementMode = false;
        }

        if (!_isFreeEditMode && wasEditing)
        {
            await SaveCharacter();
        }
    }

    private async Task SaveCharacter()
    {
        if (_character != null)
        {
            await CharacterService.SaveAsync(_character);
        }
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _cookieHeader = HttpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(_currentUserId))
        {
            _character = await CharacterService.GetCharacterByIdAsync(Id, _currentUserId);
            _availableEquipment = await EquipmentService.GetAvailableEquipmentAsync();
        }
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
            _ = await SessionClient.GetSessionActiveAsync(_character.CampaignId.Value, SessionService);
            await SessionClient.StartAsync(_character.CampaignId.Value, _character.Id, _currentUserId, _cookieHeader);

            SessionClient.CharacterUpdated += HandleCharacterUpdated;
            SessionClient.BloodlineApproved += HandleBloodlineApproved;
        }

        await TryShowRecentBloodlineApprovalToastAsync();

        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        SessionClient.CharacterUpdated -= HandleCharacterUpdated;
        SessionClient.BloodlineApproved -= HandleBloodlineApproved;
        await SessionClient.StopAsync();
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

            _character = await CharacterService.ReloadCharacterAsync(Id, _currentUserId);
            StateHasChanged();
        });
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
        var alreadyShown = await JS.InvokeAsync<string>("sessionStorageGet", key);
        if (!string.IsNullOrEmpty(alreadyShown))
        {
            return;
        }

        var bloodlineName = activeBloodline.BloodlineDefinition?.Name ?? "Bloodline";
        ToastService.Show("Bloodline approved", $"Your bloodline application for {bloodlineName} has been approved!", ToastType.Success);
        await JS.InvokeVoidAsync("sessionStorageSet", key, "1");
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
            StateHasChanged();
        });
    }

    private static void OpenReference(string traitName)
    {
        Console.WriteLine($"Show Reference for: {traitName}");
    }

    private void OpenRoller(string traitName)
    {
        _rollerTraitName = traitName;
        _rollerBaseDice = GetTraitValue(traitName);
        _isRollerOpen = true;
    }

    private int GetTraitValue(string traitName)
    {
        if (_character == null)
        {
            return 0;
        }

        var name = traitName.Replace(" ", string.Empty);
        if (RequiemNexus.Domain.TraitMetadata.IsAttribute(name))
        {
            return _character.GetAttributeRating(name);
        }

        return _character.GetSkillRating(name);
    }

    private async Task AddBeat()
    {
        if (_character != null)
        {
            await CharacterService.AddBeatAsync(_character);
        }
    }

    private async Task RemoveBeat()
    {
        if (_character != null)
        {
            await CharacterService.RemoveBeatAsync(_character);
        }
    }

    private async Task AddXP()
    {
        if (_character != null)
        {
            await CharacterService.AddXPAsync(_character);
        }
    }

    private async Task RemoveXP()
    {
        if (_character != null)
        {
            await CharacterService.RemoveXPAsync(_character);
        }
    }

    private async Task AddEquipment()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId) && _selectedEquipmentId > 0 && _selectedEquipmentQuantity > 0)
        {
            var ce = await EquipmentService.AddEquipmentAsync(_character.Id, _selectedEquipmentId, _selectedEquipmentQuantity, _currentUserId);
            ce.Equipment = _availableEquipment.FirstOrDefault(e => e.Id == _selectedEquipmentId);
            _character.CharacterEquipments.Add(ce);

            _selectedEquipmentId = 0;
            _selectedEquipmentQuantity = 1;
        }
    }

    private void OpenDevotionRoller(CharacterDevotion cd)
    {
        if (_character == null || cd.DevotionDefinition?.PoolDefinitionJson == null)
        {
            return;
        }

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            var pool = System.Text.Json.JsonSerializer.Deserialize<PoolDefinition>(cd.DevotionDefinition.PoolDefinitionJson, options);
            _rollerTraitName = cd.DevotionDefinition.Name;
            _rollerBaseDice = pool != null ? TraitResolver.ResolvePool(_character, pool) : 0;
            _isRollerOpen = true;
        }
        catch
        {
            _rollerTraitName = cd.DevotionDefinition.Name;
            _rollerBaseDice = 0;
            _isRollerOpen = true;
        }
    }

    private async Task RemoveEquipment(int ceId)
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await EquipmentService.RemoveEquipmentAsync(ceId, _currentUserId);
            var ce = _character.CharacterEquipments.FirstOrDefault(e => e.Id == ceId);
            if (ce != null)
            {
                _character.CharacterEquipments.Remove(ce);
            }
        }
    }

    private void ToggleMeritExpanded(int characterMeritId)
    {
        if (_expandedMeritIds.Contains(characterMeritId))
        {
            _ = _expandedMeritIds.Remove(characterMeritId);
        }
        else
        {
            _expandedMeritIds.Add(characterMeritId);
        }
    }

    private void HandleMeritKeydown(KeyboardEventArgs e, int characterMeritId)
    {
        if (e.Key is "Enter" or " ")
        {
            ToggleMeritExpanded(characterMeritId);
        }
    }

    private void HandleApplyBloodlineModalClosed(bool isOpen)
    {
        _isApplyBloodlineModalOpen = isOpen;
    }

    private async Task OpenApplyBloodlineModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            _eligibleBloodlines = await BloodlineService.GetEligibleBloodlinesAsync(_character.Id, _currentUserId);
            if (_eligibleBloodlines.Count == 0)
            {
                ToastService.Show("No eligible bloodlines", "No eligible bloodlines for your clan.", ToastType.Info);
                return;
            }

            _isApplyBloodlineModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task HandleApplyBloodlineRequested(int bloodlineDefinitionId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await BloodlineService.ApplyForBloodlineAsync(_character.Id, bloodlineDefinitionId, _currentUserId);
            ToastService.Show("Application submitted", "Bloodline application submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task RemoveBloodline(int characterBloodlineId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId) || _removingBloodline)
        {
            return;
        }

        _removingBloodline = true;
        try
        {
            await BloodlineService.RemoveBloodlineAsync(characterBloodlineId, _currentUserId);
            ToastService.Show("Bloodline removed", "The bloodline has been removed from your character.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _removingBloodline = false;
        }
    }

    private void HandleApplyCovenantModalClosed(bool isOpen)
    {
        _isApplyCovenantModalOpen = isOpen;
    }

    private async Task OpenApplyCovenantModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            _eligibleCovenants = await CovenantService.GetEligibleCovenantsAsync(_character.Id, _currentUserId);
            if (_eligibleCovenants.Count == 0)
            {
                ToastService.Show("No eligible covenants", "Join a campaign first, or you may already have a pending application.", ToastType.Info);
                return;
            }

            _isApplyCovenantModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task HandleApplyCovenantRequested(int covenantDefinitionId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await CovenantService.ApplyForCovenantAsync(_character.Id, covenantDefinitionId, _currentUserId);
            ToastService.Show("Application submitted", "Covenant application submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task RequestLeaveCovenant()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId) || _requestingLeave)
        {
            return;
        }

        _requestingLeave = true;
        try
        {
            await CovenantService.RequestLeaveCovenantAsync(_character.Id, _currentUserId);
            ToastService.Show("Leave requested", "Your request to leave the covenant has been submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _requestingLeave = false;
        }
    }

    private void HandleApplyLearnRiteModalClosed(bool isOpen)
    {
        _isApplyLearnRiteModalOpen = isOpen;
    }

    private async Task OpenLearnRiteModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _openingRiteModal = true;
        try
        {
            _eligibleRites = await SorceryService.GetEligibleRitesAsync(_character.Id, _currentUserId);
            if (_eligibleRites.Count == 0)
            {
                ToastService.Show("No eligible rites", "You need more Crúac or Theban Sorcery dots, or you've learned all available rites.", ToastType.Info);
                return;
            }

            _isApplyLearnRiteModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _openingRiteModal = false;
        }
    }

    private async Task HandleApplyLearnRiteRequested(int sorceryRiteDefinitionId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await SorceryService.RequestLearnRiteAsync(_character.Id, sorceryRiteDefinitionId, _currentUserId);
            ToastService.Show("Request submitted", "Rite learning request submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task OpenChosenMysteryModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            _eligibleScales = await CoilService.GetScalesAsync();
            if (_eligibleScales.Count == 0)
            {
                ToastService.Show("No scales found", "No Scale definitions found in the database.", ToastType.Info);
                return;
            }

            _isChosenMysteryModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task HandleChosenMysteryRequested(int scaleId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await CoilService.RequestChosenMysteryAsync(_character.Id, scaleId, _currentUserId);
            ToastService.Show("Request submitted", "Chosen Mystery request submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _isChosenMysteryModalOpen = false;
        }
    }

    private async Task OpenLearnCoilModal()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            _eligibleCoils = await CoilService.GetEligibleCoilsAsync(_character.Id, _currentUserId);
            if (_eligibleCoils.Count == 0)
            {
                ToastService.Show("No eligible coils", "No eligible Coils to purchase. Check prerequisites and Ordo Status.", ToastType.Info);
                return;
            }

            _isLearnCoilModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
    }

    private async Task HandleLearnCoilRequested(int coilDefinitionId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            await CoilService.RequestLearnCoilAsync(_character.Id, coilDefinitionId, _currentUserId);
            ToastService.Show("Request submitted", "Coil purchase request submitted. Awaiting Storyteller approval.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
        catch (Exception ex)
        {
            ToastService.Show("Error", ex.Message, ToastType.Error);
        }
        finally
        {
            _isLearnCoilModalOpen = false;
        }
    }

    private static string TraditionLabel(Domain.Enums.SorceryType t) =>
        t switch
        {
            Domain.Enums.SorceryType.Cruac => "Crúac",
            Domain.Enums.SorceryType.Theban => "Theban",
            Domain.Enums.SorceryType.Necromancy => "Necromancy",
            Domain.Enums.SorceryType.OrdoDraculRitual => "Ordo ritual",
            _ => t.ToString(),
        };

    private async Task OpenRiteRoller(CharacterRite cr)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            var def = cr.SorceryRiteDefinition;
            Result<IReadOnlyList<RiteRequirement>> parsed =
                RiteRequirementValidator.ParseRequirements(def?.RequirementsJson);
            IReadOnlyList<RiteRequirement> reqs = parsed.IsSuccess ? parsed.Value! : [];

            var request = new BeginRiteActivationRequest();
            if (RiteRequirementValidator.RequiresExternalAcknowledgment(reqs))
            {
                bool ok = await JS.InvokeAsync<bool>(
                    "confirm",
                    "This rite requires narrative sacrifices (focus, sacrament, offering, etc.) you must have completed at the table. Confirm to apply Vitae/Willpower/stain costs and roll.");
                if (!ok)
                {
                    return;
                }

                request = new BeginRiteActivationRequest(
                    AcknowledgePhysicalSacrament: true,
                    AcknowledgeHeart: true,
                    AcknowledgeMaterialOffering: true,
                    AcknowledgeMaterialFocus: true);
            }

            int dice = await SorceryService.BeginRiteActivationAsync(_character.Id, cr.Id, _currentUserId, request);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
            _rollerTraitName = cr.SorceryRiteDefinition?.Name ?? "Rite";
            _rollerBaseDice = dice;
            _isRollerOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Rite activation", ex.Message, ToastType.Error);
        }
    }
}

#pragma warning restore SA1214
#pragma warning restore SA1204
#pragma warning restore SA1202
#pragma warning restore SA1201
