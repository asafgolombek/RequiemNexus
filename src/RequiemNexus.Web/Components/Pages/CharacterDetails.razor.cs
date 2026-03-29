// Blazor code-behind: member ordering follows handler grouping rather than StyleCop element order.
#pragma warning disable SA1201 // Order of fields, properties, and methods
#pragma warning disable SA1202 // Public/protected/private ordering
#pragma warning disable SA1204 // Static before instance
#pragma warning disable SA1214 // Readonly fields before non-readonly

using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Models;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using RequiemNexus.Web.Components.UI;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Helpers;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages;

/// <summary>
/// Code-behind for the interactive character sheet page.
/// </summary>
public partial class CharacterDetails : IAsyncDisposable
{
    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private ILogger<CharacterDetails> Logger { get; set; } = default!;

    [Inject]
    private ITouchstoneService TouchstoneService { get; set; } = default!;

    /// <summary>Route parameter: character id.</summary>
    [Parameter]
    public int Id { get; set; }

    private Character? _character;
    private bool _isSheetLoading = true;
    private string? _sheetLoadError;
    private string? _currentUserId;
    private string? _cookieHeader;
    private PersistingComponentStateSubscription _persistingSubscription;
    private List<Asset> _availableAssets = [];
    private int _selectedAssetId = 0;
    private int _selectedAssetQuantity = 1;

    /// <summary>
    /// Whether the Pack tab Purchase action is inactive (no click is sent to the server while this is true).
    /// </summary>
    private bool IsPurchaseButtonDisabled => _selectedAssetId <= 0 || _selectedAssetQuantity < 1;

    private string PurchaseButtonTitle =>
        _availableAssets.Count == 0
            ? "No listed assets in the catalog."
            : IsPurchaseButtonDisabled
                ? "Select an item and set quantity to at least 1."
                : "Add this item to your inventory.";

    private bool _isRollerOpen = false;
    private string _rollerTraitName = string.Empty;
    private int _rollerBaseDice = 1;
    private int? _rollerFixedDicePool;
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

    /// <summary>Resolved dice pools for discipline powers that define <c>PoolDefinitionJson</c>.</summary>
    private readonly Dictionary<int, int> _disciplinePowerResolvedPools = [];

    private bool _isDisciplineActivateModalOpen;
    private DisciplinePower? _disciplineActivatePower;
    private int _disciplineActivatePool;

    private static readonly JsonSerializerOptions _poolJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

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

    /// <summary>True when the health track is entirely aggravated (VtR 2e incapacitated).</summary>
    private bool IsIncapacitated =>
        _character != null
        && WoundPenaltyResolver.IsIncapacitated(_character.HealthDamage, _character.MaxHealth);

    private bool HasTouchstoneBonus =>
        _character != null
        && (!string.IsNullOrWhiteSpace(_character.Touchstone)
            || _character.Merits.Any(m => m.Rating > 0 && string.Equals(m.Merit?.Name, "Touchstone", StringComparison.Ordinal)));

    private string RemorsePoolTooltip =>
        _character == null
            ? string.Empty
            : _character.Humanity <= 0
                ? "Pool: chance die (1 die)"
                : $"Pool: {_character.Humanity + (HasTouchstoneBonus ? 1 : 0)} dice (Humanity{(HasTouchstoneBonus ? " + Touchstone" : string.Empty)})";

    private bool _degRollPlayerModalOpen;

    private string PlayerDegenerationModalMessage =>
        _character == null
            ? string.Empty
            : _character.Humanity <= 0
                ? $"{_character.Name} rolls a degeneration chance die (Humanity 0). Success (≥1 success): clear all stains. Failure: lose 1 Humanity, clear stains; dramatic failure also applies Guilty."
                : $"{_character.Name} rolls degeneration: Resolve {_character.GetAttributeRating(AttributeId.Resolve)} + (7 − {_character.Humanity}) = {_character.GetAttributeRating(AttributeId.Resolve) + (7 - _character.Humanity)} dice. Success: clear stains, Humanity unchanged. Failure: lose 1 Humanity, clear stains; dramatic failure also applies Guilty.";

    private void OpenPlayerDegenerationModal() => _degRollPlayerModalOpen = true;

    private void ClosePlayerDegenerationModal()
    {
        _degRollPlayerModalOpen = false;
    }

    private async Task ConfirmPlayerDegenerationRollAsync()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        Result<DegenerationRollOutcome> result = await HumanityService.ExecuteDegenerationRollAsync(Id, _currentUserId);
        if (result.IsSuccess)
        {
            ToastService.Show("Degeneration", "Roll completed. Check the dice feed for results.", ToastType.Success);
            ClosePlayerDegenerationModal();
            _character = await CharacterService.ReloadCharacterAsync(Id, _currentUserId);
            await ResolveDisciplinePowerPoolsAsync();
            StateHasChanged();
        }
        else
        {
            ToastService.Show("Degeneration", result.Error ?? "Roll failed.", ToastType.Warning);
        }
    }

    private async Task RollRemorseFromSheetAsync()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        Result<DegenerationRollOutcome> result = await TouchstoneService.RollRemorseAsync(Id, _currentUserId);
        if (result.IsSuccess)
        {
            ToastService.Show("Remorse", "Roll completed. Check the dice feed for results.", ToastType.Success);
            _character = await CharacterService.ReloadCharacterAsync(Id, _currentUserId);
            await ResolveDisciplinePowerPoolsAsync();
            StateHasChanged();
        }
        else
        {
            ToastService.Show("Remorse", result.Error ?? "Roll failed.", ToastType.Warning);
        }
    }

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

    private Task GoToPackTab()
    {
        SelectTab("pack");
        return Task.CompletedTask;
    }

    private static IEnumerable<CharacterAsset> PackInventoryOrder(ICollection<CharacterAsset> assets) =>
        assets.OrderByDescending(ca => ca.IsEquipped)
            .ThenBy(ca => ca.BackpackSlotIndex ?? 100)
            .ThenBy(ca => ca.Asset?.Name ?? string.Empty);

    private static string BackpackSlotSelectValue(CharacterAsset ca) =>
        ca.BackpackSlotIndex.HasValue
            ? ca.BackpackSlotIndex.Value.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

    private void HandleTabKeydown(KeyboardEventArgs e)
    {
        var tabs = new[] { "sheet", "pack", "identity", "history" };
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

    private async Task ReloadCharacterAfterHealAsync()
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        await InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        _persistingSubscription = ApplicationState.RegisterOnPersisting(PersistCookieHeader);

        if (!ApplicationState.TryTakeFromJson<string>("rnCookieHeader", out _cookieHeader))
        {
            _cookieHeader = HttpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();
        }

        try
        {
            AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            _currentUserId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(_currentUserId))
            {
                // Detach any tracked instance from this scoped DbContext (e.g. after visiting Advancement)
                // before loading the full include graph again.
                _character = await CharacterService.ReloadCharacterAsync(Id, _currentUserId);
                _availableAssets = await CharacterAssetService.GetListedCatalogAsync();
                await ResolveDisciplinePowerPoolsAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load character sheet for character {CharacterId}", Id);
            _sheetLoadError = "Unable to load this character. Try refreshing the page or signing in again.";
            _character = null;
        }
        finally
        {
            _isSheetLoading = false;
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
            await ResolveDisciplinePowerPoolsAsync();
            StateHasChanged();
        });
    }

    private async Task OpenDisciplinePowerActivateModal(DisciplinePower power)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        try
        {
            _disciplineActivatePower = power;
            _disciplineActivatePool = await DisciplineActivationService.ResolveActivationPoolAsync(
                _character.Id,
                power.Id,
                _currentUserId);
            _isDisciplineActivateModalOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Discipline", ex.Message, ToastType.Error);
        }
    }

    private async Task HandleDisciplineActivateConfirmedAsync()
    {
        if (_character == null || _disciplineActivatePower == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _isDisciplineActivateModalOpen = false;
        int characterId = _character.Id;
        string userId = _currentUserId;
        DisciplinePower power = _disciplineActivatePower;
        try
        {
            int dice = await DisciplineActivationService.ActivatePowerAsync(characterId, power.Id, userId);
            _character = await CharacterService.ReloadCharacterAsync(characterId, userId);
            await ResolveDisciplinePowerPoolsAsync();
            _rollerTraitName = power.Name;
            _rollerBaseDice = dice;
            _rollerFixedDicePool = null;
            _isRollerOpen = true;
        }
        catch (Exception ex)
        {
            ToastService.Show("Discipline", ex.Message, ToastType.Error);
            _character = await CharacterService.ReloadCharacterAsync(characterId, userId);
        }
    }

    private async Task ResolveDisciplinePowerPoolsAsync()
    {
        _disciplinePowerResolvedPools.Clear();
        if (_character == null)
        {
            return;
        }

        foreach (CharacterDiscipline cd in _character.Disciplines)
        {
            if (cd.Discipline?.Powers == null)
            {
                continue;
            }

            foreach (DisciplinePower p in cd.Discipline.Powers)
            {
                if (string.IsNullOrEmpty(p.PoolDefinitionJson))
                {
                    continue;
                }

                try
                {
                    PoolDefinition? pool = JsonSerializer.Deserialize<PoolDefinition>(p.PoolDefinitionJson, _poolJsonOptions);
                    if (pool != null)
                    {
                        _disciplinePowerResolvedPools[p.Id] = await TraitResolver.ResolvePoolAsync(_character, pool);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        ex,
                        "Failed to resolve PoolDefinitionJson for discipline power {PowerId} on character {CharacterId}",
                        p.Id,
                        _character.Id);
                }
            }
        }
    }

    private static void OpenReference(string traitName)
    {
        Console.WriteLine($"Show Reference for: {traitName}");
    }

    private void OpenRoller(string traitName)
    {
        _rollerFixedDicePool = null;
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
        if (TraitMetadata.IsAttribute(name))
        {
            return _character.GetAttributeRating(name);
        }

        return _character.GetSkillRating(name);
    }

    private async Task AddBeat()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CharacterService.AddBeatAsync(_character.Id, _currentUserId);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
    }

    private async Task RemoveBeat()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CharacterService.RemoveBeatAsync(_character.Id, _currentUserId);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
    }

    private async Task AddXP()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CharacterService.AddXPAsync(_character.Id, _currentUserId);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
    }

    private async Task RemoveXP()
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CharacterService.RemoveXPAsync(_character.Id, _currentUserId);
            _character = await CharacterService.ReloadCharacterAsync(_character.Id, _currentUserId);
        }
    }

    private async Task PurchaseSelectedAssetAsync()
    {
        if (_character == null)
        {
            Logger.LogWarning("Purchase blocked: character not loaded for route {CharacterId}", Id);
            ToastService.Show("Pack", "Character not loaded. Try refreshing the page.", ToastType.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_currentUserId))
        {
            Logger.LogWarning("Purchase blocked: no authenticated user for character {CharacterId}", _character.Id);
            ToastService.Show("Pack", "Sign in again to purchase items.", ToastType.Warning);
            return;
        }

        if (_selectedAssetId <= 0)
        {
            Logger.LogWarning("Purchase blocked: no asset selected for character {CharacterId}", _character.Id);
            ToastService.Show("Pack", "Choose an item from the list first.", ToastType.Warning);
            return;
        }

        if (_selectedAssetQuantity <= 0)
        {
            Logger.LogWarning(
                "Purchase blocked: invalid quantity {Quantity} for character {CharacterId}",
                _selectedAssetQuantity,
                _character.Id);
            ToastService.Show("Pack", "Enter a quantity of at least 1.", ToastType.Warning);
            return;
        }

        int purchaseCharacterId = _character.Id;
        try
        {
            AssetProcurementStartResult r = await AssetProcurementService.BeginProcurementAsync(
                purchaseCharacterId,
                _selectedAssetId,
                _selectedAssetQuantity,
                _currentUserId,
                playerNote: null);

            switch (r.Outcome)
            {
                case AssetProcurementOutcome.AddedImmediately:
                    ToastService.Show("Acquired", r.Message ?? "Item added.", ToastType.Success);
                    _character = await CharacterService.ReloadCharacterAsync(purchaseCharacterId, _currentUserId);
                    _selectedAssetId = 0;
                    _selectedAssetQuantity = 1;
                    break;
                case AssetProcurementOutcome.AddedByReach:
                    ToastService.Show("The Reach", r.Message ?? "Item acquired using your Reach for this chapter.", ToastType.Success);
                    _character = await CharacterService.ReloadCharacterAsync(purchaseCharacterId, _currentUserId);
                    _selectedAssetId = 0;
                    _selectedAssetQuantity = 1;
                    break;
                case AssetProcurementOutcome.AwaitingStorytellerApproval:
                    ToastService.Show("Pending approval", r.Message ?? "Storyteller notified.", ToastType.Info);
                    _selectedAssetId = 0;
                    _selectedAssetQuantity = 1;
                    break;
                case AssetProcurementOutcome.Blocked:
                    ToastService.Show("Purchase", r.Message ?? "This action cannot be completed.", ToastType.Warning);
                    break;
                default:
                    Logger.LogWarning(
                        "Unhandled procurement outcome {Outcome} for character {CharacterId}",
                        r.Outcome,
                        purchaseCharacterId);
                    ToastService.Show(
                        "Purchase",
                        "Something went wrong. Try again or contact support.",
                        ToastType.Error);
                    break;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning(ex, "Purchase denied for character {CharacterId}", purchaseCharacterId);
            ToastService.Show(
                "Purchase",
                "You can only purchase on your own character (or as Storyteller where allowed).",
                ToastType.Error);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Purchase failed for character {CharacterId}", purchaseCharacterId);
            ToastService.Show("Purchase", ex.Message, ToastType.Error);
        }
    }

    private bool _isForgeModalOpen = false;
    private CharacterAsset? _forgeTarget;
    private List<AssetModifier> _availableModifiers = [];

    private void OpenRepairRoller(CharacterAsset ca)
    {
        if (_character == null)
        {
            return;
        }

        _rollerTraitName = $"Repair {ca.Asset?.Name} (Wits + Crafts)";
        _rollerBaseDice = _character.GetAttributeRating(AttributeId.Wits) + _character.GetSkillRating(SkillId.Crafts);
        _rollerFixedDicePool = null;
        _isRollerOpen = true;
    }

    private async Task OpenForgeModal(CharacterAsset ca)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        _forgeTarget = ca;

        // In a real implementation, we'd fetch modifiers from a service.
        // For now, we'll just open the modal.
        _isForgeModalOpen = true;
    }

    private async Task OpenDevotionRollerAsync(CharacterDevotion cd)
    {
        if (_character == null || cd.DevotionDefinition?.PoolDefinitionJson == null)
        {
            return;
        }

        _rollerFixedDicePool = null;
        _rollerTraitName = cd.DevotionDefinition.Name;
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            PoolDefinition? pool = System.Text.Json.JsonSerializer.Deserialize<PoolDefinition>(cd.DevotionDefinition.PoolDefinitionJson, options);
            _rollerFixedDicePool = pool != null ? await TraitResolver.ResolvePoolAsync(_character, pool) : 0;
            _rollerBaseDice = 0;
            _isRollerOpen = true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Failed to resolve devotion pool for {DevotionName} on character {CharacterId}; opening roller with 0 dice",
                cd.DevotionDefinition.Name,
                Id);
            _rollerBaseDice = 0;
            _isRollerOpen = true;
        }
    }

    private async Task RemoveCharacterAsset(int characterAssetId)
    {
        if (_character != null && !string.IsNullOrEmpty(_currentUserId))
        {
            await CharacterAssetService.RemoveCharacterAssetAsync(characterAssetId, _currentUserId);
            CharacterAsset? row = _character.CharacterAssets.FirstOrDefault(e => e.Id == characterAssetId);
            if (row != null)
            {
                _character.CharacterAssets.Remove(row);
            }
        }
    }

    private async Task OnAssetEquippedChanged(CharacterAsset ca, ChangeEventArgs e)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        bool isEquipped = e.Value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out bool bs) => bs,
            _ => ca.IsEquipped,
        };

        int characterId = _character.Id;
        try
        {
            await CharacterAssetService.SetEquippedAsync(ca.Id, isEquipped, _currentUserId);
            _character = await CharacterService.ReloadCharacterAsync(characterId, _currentUserId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Equip toggle failed for character asset {CharacterAssetId}", ca.Id);
            ToastService.Show("Pack", ex.Message, ToastType.Warning);
            _character = await CharacterService.ReloadCharacterAsync(characterId, _currentUserId);
        }
    }

    private async Task OnStructureChanged(CharacterAsset ca, ChangeEventArgs e)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        string? raw = e.Value?.ToString();
        int? structure = string.IsNullOrWhiteSpace(raw) ? null : int.TryParse(raw, out int n) ? n : ca.CurrentStructure;
        await CharacterAssetService.SetCurrentStructureAsync(ca.Id, structure, _currentUserId);
        ca.CurrentStructure = structure;
    }

    private async Task OnBackpackSlotSelectAsync(CharacterAsset ca, ChangeEventArgs e)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        string? raw = e.Value?.ToString();
        int? slot = string.IsNullOrEmpty(raw) ? null : int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : ca.BackpackSlotIndex;

        int characterId = _character.Id;
        try
        {
            await CharacterAssetService.SetBackpackSlotAsync(ca.Id, slot, _currentUserId);
            _character = await CharacterService.ReloadCharacterAsync(characterId, _currentUserId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Backpack assign failed for character asset {CharacterAssetId}", ca.Id);
            ToastService.Show("Pack", ex.Message, ToastType.Warning);
            _character = await CharacterService.ReloadCharacterAsync(characterId, _currentUserId);
        }
    }

    private async Task ClearBackpackSlotAsync(int characterAssetId)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return;
        }

        int characterId = _character.Id;
        try
        {
            await CharacterAssetService.SetBackpackSlotAsync(characterAssetId, null, _currentUserId);
            _character = await CharacterService.ReloadCharacterAsync(characterId, _currentUserId);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Backpack clear failed for character asset {CharacterAssetId}", characterAssetId);
            ToastService.Show("Pack", ex.Message, ToastType.Warning);
            _character = await CharacterService.ReloadCharacterAsync(characterId, _currentUserId);
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

            int dice = await SorceryActivationService.BeginRiteActivationAsync(_character.Id, cr.Id, _currentUserId, request);
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
