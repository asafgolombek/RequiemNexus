// Blazor partial: Pack tab procurement, forge modal, and inventory mutations for CharacterDetails.
using System.Globalization;
using Microsoft.AspNetCore.Components;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;
using RequiemNexus.Web.Enums;
using RequiemNexus.Web.Services;

namespace RequiemNexus.Web.Components.Pages;

public partial class CharacterDetails
{
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

    private Task OpenForgeModal(CharacterAsset ca)
    {
        if (_character == null || string.IsNullOrEmpty(_currentUserId))
        {
            return Task.CompletedTask;
        }

        _forgeTarget = ca;

        // In a real implementation, we'd fetch modifiers from a service.
        // For now, we'll just open the modal.
        _isForgeModalOpen = true;
        return Task.CompletedTask;
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
}
