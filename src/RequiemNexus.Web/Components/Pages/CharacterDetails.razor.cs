// Blazor code-behind: lifecycle, sheet navigation, and humanity UI (see sibling partials for Session, pools, progression, modals, etc.).
#pragma warning disable SA1201 // Order of fields, properties, and methods
#pragma warning disable SA1202 // Public/protected/private ordering
#pragma warning disable SA1204 // Static before instance
#pragma warning disable SA1214 // Readonly fields before non-readonly

using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Models;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Helpers;

namespace RequiemNexus.Web.Components.Pages;

/// <summary>
/// Code-behind for the interactive character sheet page.
/// </summary>
public partial class CharacterDetails : IAsyncDisposable
{
    /// <summary>Route parameter: character id.</summary>
    [Parameter]
    public int Id { get; set; }

    /// <summary>True when the character may use the Blood Sorcery sheet section (Crúac/Theban covenant or Necromancy dots).</summary>
    private bool ShowBloodSorcerySection =>
        _character != null
        && _character.CovenantJoinStatus == null
        && (
            (_character.Covenant?.SupportsBloodSorcery == true)
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

    private string PlayerDegenerationModalMessage =>
        _character == null
            ? string.Empty
            : _character.Humanity <= 0
                ? $"{_character.Name} rolls a degeneration chance die (Humanity 0). Success (≥1 success): clear all stains. Failure: lose 1 Humanity, clear stains; dramatic failure also applies Guilty."
                : $"{_character.Name} rolls degeneration: Resolve {_character.GetAttributeRating(AttributeId.Resolve)} + (7 − {_character.Humanity}) = {_character.GetAttributeRating(AttributeId.Resolve) + (7 - _character.Humanity)} dice. Success: clear stains, Humanity unchanged. Failure: lose 1 Humanity, clear stains; dramatic failure also applies Guilty.";

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

    private Task BeginEditingNameAsync()
    {
        _isEditingName = true;
        return Task.CompletedTask;
    }

    private Task OnTabBarSelectAsync(string tab)
    {
        SelectTab(tab);
        return Task.CompletedTask;
    }

    private Task HandleTabBarKeydownAsync(KeyboardEventArgs e)
    {
        HandleTabKeydown(e);
        return Task.CompletedTask;
    }

    private void OnTraitLabelClick(string traitName) => OpenReference(traitName);

    private void OnTraitRollClick(string traitName) => OpenRoller(traitName);

    private Task OnAttributeOrSkillDotsChangedAsync((string TraitName, int NewValue) change) => SaveCharacter();

    private Task HandlePackAssetEquippedAsync((CharacterAsset Asset, ChangeEventArgs Args) x) =>
        OnAssetEquippedChanged(x.Asset, x.Args);

    private Task HandlePackStructureChangedAsync((CharacterAsset Asset, ChangeEventArgs Args) x) =>
        OnStructureChanged(x.Asset, x.Args);

    private Task HandlePackBackpackSlotSelectAsync((CharacterAsset Asset, ChangeEventArgs Args) x) =>
        OnBackpackSlotSelectAsync(x.Asset, x.Args);

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
}

#pragma warning restore SA1214
#pragma warning restore SA1204
#pragma warning restore SA1202
#pragma warning restore SA1201
